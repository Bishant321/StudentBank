using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class OpenBankingService(HttpClient httpClient, IConfiguration configuration)
{
    private const string AuthHost = "https://auth.truelayer-sandbox.com";
    private const string DataHost = "https://api.truelayer-sandbox.com";
    private static readonly ConcurrentDictionary<string, Connection> Connections = new();
    private static readonly ConcurrentDictionary<string, PendingState> PendingStates = new();

    public ConnectionStart CreateAuthorizationUrl(string userId)
    {
        var clientId = RequiredSetting("TrueLayer:ClientId", "TRUELAYER_CLIENT_ID");
        var redirectUri = configuration["TrueLayer:RedirectUri"] ?? configuration["TRUELAYER_REDIRECT_URI"]
            ?? "https://console.truelayer.com/redirect-page";
        var state = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        PendingStates[userId] = new PendingState(state, DateTimeOffset.UtcNow.AddMinutes(10));

        var query = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["scope"] = "info accounts balance transactions",
            ["providers"] = "uk-ob-all",
            ["state"] = state
        };
        var url = $"{AuthHost}/?{string.Join("&", query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"))}";
        return new ConnectionStart(url, state);
    }

    public async Task ExchangeCodeAsync(string userId, string state, string code, CancellationToken cancellationToken)
    {
        if (!PendingStates.TryGetValue(userId, out var pending)
            || pending.ExpiresAt <= DateTimeOffset.UtcNow
            || !System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(pending.Value), System.Text.Encoding.UTF8.GetBytes(state)))
        {
            throw new InvalidOperationException("The bank connection has expired. Start the connect flow again.");
        }

        PendingStates.TryRemove(userId, out _);
        var redirectUri = configuration["TrueLayer:RedirectUri"] ?? configuration["TRUELAYER_REDIRECT_URI"]
            ?? "https://console.truelayer.com/redirect-page";
        using var response = await httpClient.PostAsync($"{AuthHost}/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = RequiredSetting("TrueLayer:ClientId", "TRUELAYER_CLIENT_ID"),
            ["client_secret"] = RequiredSetting("TrueLayer:ClientSecret", "TRUELAYER_CLIENT_SECRET"),
            ["redirect_uri"] = redirectUri,
            ["code"] = code
        }), cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("The bank provider rejected the authorization code. Start a new connection and try again.");

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The bank provider returned an empty token response.");
        Connections[userId] = new Connection(token.AccessToken, DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn - 30));
    }

    public async Task<IReadOnlyList<ExternalAccount>> GetAccountsAsync(string userId, CancellationToken cancellationToken)
    {
        var connection = GetConnection(userId);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{DataHost}/data/v1/accounts");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                Connections.TryRemove(userId, out _);
            throw new InvalidOperationException("Could not read connected bank accounts. Reconnect the bank and try again.");
        }

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var accounts = new List<ExternalAccount>();
        foreach (var item in document.RootElement.GetProperty("results").EnumerateArray())
        {
            var accountId = ReadString(item, "account_id");
            if (string.IsNullOrWhiteSpace(accountId)) continue;
            var balance = await GetBalanceAsync(connection.AccessToken, accountId, cancellationToken);
            var transactions = await GetRecentTransactionsAsync(connection.AccessToken, accountId, cancellationToken);
            accounts.Add(new ExternalAccount(
                ReadNestedString(item, "provider", "display_name") ?? "Connected bank",
                ReadString(item, "display_name") ?? ReadString(item, "account_type") ?? "Bank account",
                ReadString(item, "currency") ?? balance.Currency,
                balance.Amount,
                transactions));
        }
        return accounts;
    }

    public void Disconnect(string userId) => Connections.TryRemove(userId, out _);

    public sealed record ConnectionStart(string AuthorizationUrl, string State);

    private async Task<(decimal Amount, string Currency)> GetBalanceAsync(string accessToken, string accountId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{DataHost}/data/v1/accounts/{Uri.EscapeDataString(accountId)}/balance");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var result = document.RootElement.GetProperty("results").EnumerateArray().FirstOrDefault();
        var amount = result.ValueKind == JsonValueKind.Object && result.TryGetProperty("available", out var available)
            ? available.GetDecimal()
            : result.ValueKind == JsonValueKind.Object && result.TryGetProperty("current", out var current) ? current.GetDecimal() : 0m;
        var currency = result.ValueKind == JsonValueKind.Object ? ReadString(result, "currency") ?? "GBP" : "GBP";
        return (amount, currency);
    }

    private async Task<IReadOnlyList<ExternalTransaction>> GetRecentTransactionsAsync(string accessToken, string accountId, CancellationToken cancellationToken)
    {
        var from = DateTime.UtcNow.AddDays(-30).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var to = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"{DataHost}/data/v1/accounts/{Uri.EscapeDataString(accountId)}/transactions?from={from}&to={to}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return document.RootElement.GetProperty("results").EnumerateArray()
            .Select(item => new ExternalTransaction(
                ReadString(item, "description") ?? "Transaction",
                ReadString(item, "transaction_category") ?? "Other",
                item.TryGetProperty("amount", out var amount) && amount.TryGetDecimal(out var value) ? value : 0m,
                ReadString(item, "currency") ?? "GBP",
                item.TryGetProperty("timestamp", out var timestamp) && timestamp.TryGetDateTimeOffset(out var date) ? date : DateTimeOffset.UtcNow))
            .ToList();
    }

    private Connection GetConnection(string userId)
    {
        if (!Connections.TryGetValue(userId, out var connection) || connection.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            Connections.TryRemove(userId, out _);
            throw new InvalidOperationException("Connect a bank account first. Sandbox connections are kept in memory and expire when the server restarts or the access token expires.");
        }
        return connection;
    }

    private string RequiredSetting(string configKey, string environmentKey) =>
        configuration[configKey] ?? configuration[environmentKey]
        ?? throw new InvalidOperationException($"Set {environmentKey} on the StudentBank server to enable the TrueLayer sandbox.");

    private static string? ReadString(JsonElement item, string property) =>
        item.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string? ReadNestedString(JsonElement item, string parent, string property) =>
        item.TryGetProperty(parent, out var value) ? ReadString(value, property) : null;

    private sealed record PendingState(string Value, DateTimeOffset ExpiresAt);
    private sealed record Connection(string AccessToken, DateTimeOffset ExpiresAt);
    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
    public sealed record ExternalAccount(string Provider, string Name, string Currency, decimal Balance, IReadOnlyList<ExternalTransaction> Transactions);
    public sealed record ExternalTransaction(string Description, string Category, decimal Amount, string Currency, DateTimeOffset Timestamp);
}
