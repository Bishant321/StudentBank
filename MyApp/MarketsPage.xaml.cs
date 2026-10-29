using Microsoft.Extensions.DependencyInjection;
using MyApp.Models;
using MyApp.Services;

namespace MyApp;

public partial class MarketsPage : ContentPage
{
    private readonly BankingApiClient api = App.ServiceProvider.GetRequiredService<BankingApiClient>();
    private MarketQuote? currentQuote;

    public MarketsPage() => InitializeComponent();

    private async void OnGetQuoteClicked(object? sender, EventArgs e)
    {
        var symbol = SymbolEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(symbol))
        {
            await DisplayAlertAsync("Symbol needed", "Enter a market symbol such as IBM or AAPL.", "OK");
            return;
        }

        QuoteError.IsVisible = false;
        QuoteIndicator.IsVisible = QuoteIndicator.IsRunning = true;
        try
        {
            currentQuote = await api.GetMarketQuoteAsync(symbol);
            QuoteTitle.Text = currentQuote.Symbol;
            QuoteNumbers.Text = $"{currentQuote.Price:N2} · {currentQuote.Change:+0.00;-0.00;0.00} ({currentQuote.ChangePercent})";
            QuoteDate.Text = $"Latest trading day: {currentQuote.TradingDay}";
            QuoteNotice.Text = currentQuote.DataNotice;
            QuotePanel.IsVisible = true;
            ExplanationLabel.IsVisible = false;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            QuoteError.Text = exception.Message;
            QuoteError.IsVisible = true;
            currentQuote = null;
        }
        finally
        {
            QuoteIndicator.IsVisible = QuoteIndicator.IsRunning = false;
        }
    }

    private async void OnExplainClicked(object? sender, EventArgs e)
    {
        if (currentQuote is null)
        {
            await DisplayAlertAsync("Get a quote first", "Load a market quote before asking the assistant to explain it.", "OK");
            return;
        }

        ExplainIndicator.IsVisible = ExplainIndicator.IsRunning = true;
        ExplanationLabel.IsVisible = false;
        try
        {
            ExplanationLabel.Text = await api.ExplainMarketQuoteAsync(currentQuote.Symbol,
                string.IsNullOrWhiteSpace(QuestionEntry.Text) ? "Explain the fields in this quote." : QuestionEntry.Text.Trim());
            ExplanationLabel.IsVisible = true;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            ExplanationLabel.Text = exception.Message;
            ExplanationLabel.IsVisible = true;
        }
        finally
        {
            ExplainIndicator.IsVisible = ExplainIndicator.IsRunning = false;
        }
    }
}
