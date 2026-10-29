using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;

public sealed class MarketDataService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
{
    public async Task<MarketQuote> GetQuoteAsync(string symbol, CancellationToken cancellationToken)
    {
        symbol = symbol.Trim().ToUpperInvariant();
        if (symbol.Length is < 1 or > 10 || symbol.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '.' && character != '-'))
            throw new InvalidOperationException("Enter a valid market symbol, such as IBM or AAPL.");

        var apiKey = configuration["MarketData:ApiKey"] ?? configuration["ALPHA_VANTAGE_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Market quotes are not configured. Set ALPHA_VANTAGE_API_KEY on the StudentBank server.");

        var url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={Uri.EscapeDataString(symbol)}&apikey={Uri.EscapeDataString(apiKey)}";
        using var document = await httpClientFactory.CreateClient().GetFromJsonAsync<JsonDocument>(url, cancellationToken)
            ?? throw new InvalidOperationException("The market data provider returned an empty response.");
        using (document)
        {
            if (!document.RootElement.TryGetProperty("Global Quote", out var quote) || !quote.EnumerateObject().Any())
            {
                var message = document.RootElement.TryGetProperty("Note", out var note) ? note.GetString()
                    : document.RootElement.TryGetProperty("Error Message", out var error) ? error.GetString()
                    : "No quote was found. Check the symbol or provider availability.";
                throw new InvalidOperationException(message ?? "No quote was found.");
            }

            return new MarketQuote(
                symbol,
                ReadDecimal(quote, "05. price"),
                ReadDecimal(quote, "09. change"),
                ReadString(quote, "10. change percent"),
                ReadString(quote, "07. latest trading day"),
                "External market data. Timing depends on the provider's entitlement; this is not an execution price.");
        }
    }

    private static decimal ReadDecimal(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value)
        && decimal.TryParse(value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;

    private static string ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) ? value.GetString() ?? string.Empty : string.Empty;
}

public sealed record MarketQuote(string Symbol, decimal Price, decimal Change, string ChangePercent, string TradingDay, string DataNotice);
