using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Models;
using MyApp.Services;

namespace MyApp;

public partial class MainPage : ContentPage
{
    private readonly BankingApiClient bankingApi = App.ServiceProvider.GetRequiredService<BankingApiClient>();
    private bool balanceVisible = true;

    public ObservableCollection<BankTransactionDisplay> Transactions { get; } = new();

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
        GreetingName.Text = bankingApi.CurrentUserName;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var accounts = await bankingApi.GetAccountsAsync();
            if (accounts.Count == 0)
            {
                BalanceLabel.Text = "No accounts";
                AccountOneName.Text = "Create an account";
                AccountOneNumber.Text = "Open Accounts";
                AccountTwoNumber.Text = "—";
                Transactions.Clear();
                return;
            }

            var mainAccount = accounts[0];
            GreetingName.Text = string.IsNullOrWhiteSpace(bankingApi.CurrentUserName) ? mainAccount.HolderName : bankingApi.CurrentUserName;
            BalanceLabel.Text = $"{mainAccount.Currency} {mainAccount.Balance:N2}";
            BalanceCaption.Text = "StudentBank demo ledger · not an external bank balance";
            AccountOneName.Text = mainAccount.HolderName;
            AccountOneNumber.Text = $"•• {mainAccount.AccountNumber[^Math.Min(4, mainAccount.AccountNumber.Length)..]}";
            var additional = accounts.Count - 1;
            AccountTwoNumber.Text = additional == 0 ? "None yet" : $"{additional} more account{(additional == 1 ? "" : "s")}";

            var recent = new List<(DateTime Timestamp, string Currency, StatementTransaction Transaction)>();
            foreach (var account in accounts)
            {
                var statement = await bankingApi.GetStatementAsync(account.AccountNumber);
                recent.AddRange(statement.Transactions.Select(transaction => (transaction.Timestamp, statement.Currency, transaction)));
            }

            Transactions.Clear();
            foreach (var item in recent.OrderByDescending(item => item.Timestamp).Take(4))
            {
                var incoming = item.Transaction.Type.Equals("deposit", StringComparison.OrdinalIgnoreCase)
                    || item.Transaction.Type.Equals("transfer_in", StringComparison.OrdinalIgnoreCase);
                var sign = incoming ? "+" : "−";
                var color = incoming ? "#08765B" : "#263A34";
                var icon = incoming ? "↙" : "↗";
                Transactions.Add(new BankTransactionDisplay(
                    item.Transaction.Description,
                    $"{item.Timestamp:ddd, d MMM} · {item.Transaction.Type}",
                    $"{sign}{item.Currency} {item.Transaction.Amount:N2}", icon, color));
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or TaskCanceledException)
        {
            BalanceCaption.Text = "Could not refresh the demo ledger. Open Accounts to retry.";
        }
    }

    private void OnToggleBalanceClicked(object? sender, EventArgs e)
    {
        balanceVisible = !balanceVisible;
        if (balanceVisible)
            _ = OnBalanceRestoreAsync();
        else
            BalanceLabel.Text = "••••••••";
        if (sender is Button button)
            button.Text = balanceVisible ? "Hide" : "Show";
    }

    private async Task OnBalanceRestoreAsync()
    {
        try
        {
            var accounts = await bankingApi.GetAccountsAsync();
            BalanceLabel.Text = accounts.Count == 0 ? "No accounts" : $"{accounts[0].Currency} {accounts[0].Balance:N2}";
        }
        catch
        {
            BalanceLabel.Text = "Unavailable";
        }
    }

    private async void OnActionClicked(object? sender, EventArgs e)
    {
        var action = (sender as Button)?.CommandParameter?.ToString() ?? "This action";
        if (action == "Send money")
        {
            await Shell.Current.GoToAsync(nameof(TransferPage));
            return;
        }
        await DisplayAlertAsync(action, "This is a coursework demo using sample data. No real money moves in this prototype.", "OK");
    }

    private async void OnSignOutClicked(object? sender, EventArgs e)
    {
        try { await bankingApi.SignOutAsync(); }
        finally
        {
            if (Application.Current?.Windows.Count > 0)
                Application.Current.Windows[0].Page = new NavigationPage(new SignInPage());
        }
    }

    private async void OnAssistantClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//Assistant");

    private async void OnAccountsClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//Accounts");
}
