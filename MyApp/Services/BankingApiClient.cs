using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MyApp.Models;

namespace MyApp.Services;

public sealed class BankingApiClient
{
    private readonly CookieContainer cookies = new();
    private readonly HttpClient httpClient;

    public BankingApiClient()
    {
        var handler = new HttpClientHandler { CookieContainer = cookies, UseCookies = true };
#if ANDROID
        var baseAddress = "http://10.0.2.2:5080/";
#else
        var baseAddress = "http://localhost:5080/";
#endif
        httpClient = new HttpClient(handler) { BaseAddress = new Uri(baseAddress), Timeout = TimeSpan.FromSeconds(35) };
    }

    public string CurrentUserName { get; private set; } = "";
    public string CurrentUserId { get; private set; } = "";
    public bool LastLedgerReadWasOffline { get; private set; }
    public DateTimeOffset? LastOfflineLedgerSave { get; private set; }

    public async Task SignInAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/auth/login", new { email, password }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var user = document.RootElement.GetProperty("user");
        CurrentUserName = user.GetProperty("fullName").GetString() ?? "Student";
        CurrentUserId = user.GetProperty("id").GetString() ?? "";
    }

    public async Task RegisterAsync(string fullName, string email, string password, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/auth/register", new { fullName, email, password }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var user = document.RootElement.GetProperty("user");
        CurrentUserName = user.GetProperty("fullName").GetString() ?? fullName;
        CurrentUserId = user.GetProperty("id").GetString() ?? "";
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        var userId = CurrentUserId;
        CurrentUserName = "";
        CurrentUserId = "";
        LastLedgerReadWasOffline = false;
        LastOfflineLedgerSave = null;
        cookies.GetCookies(httpClient.BaseAddress!).Cast<Cookie>().ToList().ForEach(cookie => cookie.Expired = true);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            try { SecureStorage.Default.Remove(GetLedgerCacheKey(userId)); }
            catch (Exception exception) when (exception is PlatformNotSupportedException or InvalidOperationException or CryptographicException) { }
        }

        HttpResponseMessage? response = null;
        try { response = await httpClient.PostAsync("api/auth/logout", null, cancellationToken); }
        catch (HttpRequestException) { /* Local credentials and cached data are already cleared. */ }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested) { /* Local credentials and cached data are already cleared. */ }
        if (response is null) return;
        using (response)
            await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<List<BankingAccount>> GetAccountsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var accounts = await GetAsync<List<BankingAccount>>("api/accounts", cancellationToken) ?? new();
            var cache = await ReadLedgerCacheAsync(cancellationToken) ?? new OfflineLedgerCache();
            cache.Accounts = accounts;
            cache.AccountsSavedAt = DateTimeOffset.UtcNow;
            await SaveLedgerCacheAsync(cache, cancellationToken);
            LastLedgerReadWasOffline = false;
            LastOfflineLedgerSave = null;
            return accounts;
        }
        catch (Exception exception) when (IsConnectionFailure(exception, cancellationToken))
        {
            var cache = await ReadLedgerCacheAsync(cancellationToken);
            if (cache is null || cache.AccountsSavedAt is null) throw;
            LastLedgerReadWasOffline = true;
            LastOfflineLedgerSave = cache.AccountsSavedAt;
            return cache.Accounts;
        }
    }

    public async Task<AccountStatement> GetStatementAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var statement = await GetAsync<AccountStatement>($"api/accounts/{Uri.EscapeDataString(accountNumber)}/statement", cancellationToken)
                ?? throw new InvalidOperationException("The account statement was empty.");
            var cache = await ReadLedgerCacheAsync(cancellationToken) ?? new OfflineLedgerCache();
            cache.Statements[accountNumber] = statement;
            cache.StatementSavedAt[accountNumber] = DateTimeOffset.UtcNow;
            await SaveLedgerCacheAsync(cache, cancellationToken);
            LastLedgerReadWasOffline = false;
            LastOfflineLedgerSave = null;
            return statement;
        }
        catch (Exception exception) when (IsConnectionFailure(exception, cancellationToken))
        {
            var cache = await ReadLedgerCacheAsync(cancellationToken);
            if (cache is null || !cache.Statements.TryGetValue(accountNumber, out var statement)) throw;
            LastLedgerReadWasOffline = true;
            LastOfflineLedgerSave = cache.StatementSavedAt.TryGetValue(accountNumber, out var savedAt)
                ? savedAt
                : cache.AccountsSavedAt;
            return statement;
        }
    }

    public async Task CreateAccountAsync(string holderName, decimal initialDeposit, string currency, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/accounts", new { holderName, initialDeposit, currency }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task TransferAsync(string fromAccountNumber, string toAccountNumber, decimal amount, string description, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/transfer", new { fromAccountNumber, toAccountNumber, amount, description }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<(string AuthorizationUrl, string State)> BeginExternalBankConnectionAsync(CancellationToken cancellationToken = default)
    {
        var result = await GetAsync<ConnectResponse>("api/open-banking/connect", cancellationToken)
            ?? throw new InvalidOperationException("The bank connection could not be started.");
        return (result.AuthorizationUrl, result.State);
    }

    public async Task CompleteExternalBankConnectionAsync(string state, string code, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/open-banking/exchange", new { state, code }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<List<ExternalBankAccount>> GetExternalBankAccountsAsync(CancellationToken cancellationToken = default) =>
        await GetAsync<List<ExternalBankAccount>>("api/open-banking/accounts", cancellationToken) ?? new();

    public async Task DisconnectExternalBankAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync("api/open-banking/connection", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<MarketQuote> GetMarketQuoteAsync(string symbol, CancellationToken cancellationToken = default) =>
        await GetAsync<MarketQuote>($"api/market/quote/{Uri.EscapeDataString(symbol)}", cancellationToken)
        ?? throw new InvalidOperationException("No market quote was returned.");

    public async Task<string> GetSpendingInsightsAsync(string question, bool consent, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/assistant/spending-insights",
            new { message = question, consentToShareTransactions = consent }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<AssistantReply>(cancellationToken: cancellationToken);
        return result?.Reply ?? throw new InvalidOperationException("The assistant returned an empty reply.");
    }

    public async Task<string> ExplainMarketQuoteAsync(string symbol, string question, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/assistant/market-explanation", new { symbol, message = question }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<AssistantReply>(cancellationToken: cancellationToken);
        return result?.Reply ?? throw new InvalidOperationException("The assistant returned an empty reply.");
    }

    private async Task<T?> GetAsync<T>(string route, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(route, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    private async Task<OfflineLedgerCache?> ReadLedgerCacheAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(CurrentUserId)) return null;
        try
        {
            var json = await SecureStorage.Default.GetAsync(GetLedgerCacheKey(CurrentUserId));
            return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<OfflineLedgerCache>(json);
        }
        catch (Exception exception) when (exception is PlatformNotSupportedException or InvalidOperationException or CryptographicException or JsonException)
        {
            return null;
        }
    }

    private async Task SaveLedgerCacheAsync(OfflineLedgerCache cache, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(CurrentUserId)) return;
        var json = JsonSerializer.Serialize(cache);
        try { await SecureStorage.Default.SetAsync(GetLedgerCacheKey(CurrentUserId), json); }
        catch (Exception exception) when (exception is PlatformNotSupportedException or InvalidOperationException or CryptographicException) { }
    }

    private static string GetLedgerCacheKey(string userId)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(userId)));
        return $"studentbank-demo-ledger-{hash}";
    }

    private static bool IsConnectionFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException { StatusCode: null }
        || exception is TaskCanceledException && !cancellationToken.IsCancellationRequested
        || exception is IOException;

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = $"Server request failed ({(int)response.StatusCode}).";
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("message", out var direct)) message = direct.GetString() ?? message;
            else if (document.RootElement.TryGetProperty("detail", out var detail)) message = detail.GetString() ?? message;
            else if (document.RootElement.TryGetProperty("title", out var title)) message = title.GetString() ?? message;
        }
        catch (JsonException) { }
        throw new HttpRequestException(message, null, response.StatusCode);
    }

    private sealed class ConnectResponse
    {
        public string AuthorizationUrl { get; set; } = "";
        public string State { get; set; } = "";
    }

    private sealed class AssistantReply
    {
        public string Reply { get; set; } = "";
    }

    private sealed class OfflineLedgerCache
    {
        public OfflineLedgerCache() { }
        public List<BankingAccount> Accounts { get; set; } = new();
        public DateTimeOffset? AccountsSavedAt { get; set; }
        public Dictionary<string, AccountStatement> Statements { get; set; } = new();
        public Dictionary<string, DateTimeOffset> StatementSavedAt { get; set; } = new();
    }
}
