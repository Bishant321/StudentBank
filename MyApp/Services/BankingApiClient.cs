using System.Net;
using System.Net.Http.Json;
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

    public async Task SignInAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/auth/login", new { email, password }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        CurrentUserName = document.RootElement.GetProperty("user").GetProperty("fullName").GetString() ?? "Student";
    }

    public async Task RegisterAsync(string fullName, string email, string password, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/auth/register", new { fullName, email, password }, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        CurrentUserName = document.RootElement.GetProperty("user").GetProperty("fullName").GetString() ?? fullName;
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsync("api/auth/logout", null, cancellationToken);
        CurrentUserName = "";
        cookies.GetCookies(httpClient.BaseAddress!).Cast<Cookie>().ToList().ForEach(cookie => cookie.Expired = true);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<List<BankingAccount>> GetAccountsAsync(CancellationToken cancellationToken = default) =>
        await GetAsync<List<BankingAccount>>("api/accounts", cancellationToken) ?? new();

    public async Task<AccountStatement> GetStatementAsync(string accountNumber, CancellationToken cancellationToken = default) =>
        await GetAsync<AccountStatement>($"api/accounts/{Uri.EscapeDataString(accountNumber)}/statement", cancellationToken)
        ?? throw new InvalidOperationException("The account statement was empty.");

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
}
