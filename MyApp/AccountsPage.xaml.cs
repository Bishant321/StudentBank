using System.Collections.ObjectModel;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Models;
using MyApp.Services;

namespace MyApp;

public partial class AccountsPage : ContentPage
{
    private readonly BankingApiClient api = App.ServiceProvider.GetRequiredService<BankingApiClient>();
    private string pendingState = "";
    private bool isLoading;

    public ObservableCollection<BankingAccount> DemoAccounts { get; } = new();
    public ObservableCollection<ExternalBankAccount> ExternalAccounts { get; } = new();

    public AccountsPage()
    {
        InitializeComponent();
        BindingContext = this;
        CurrencyPicker.SelectedIndex = 0;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await RefreshAsync();
    }

    private async void OnRefreshClicked(object? sender, EventArgs e) => await RefreshAsync();

    private async Task RefreshAsync()
    {
        if (isLoading) return;
        isLoading = true;
        try
        {
            var accounts = await api.GetAccountsAsync();
            DemoAccounts.Clear();
            foreach (var account in accounts) DemoAccounts.Add(account);
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("Accounts unavailable", exception.Message, "OK");
        }

        try
        {
            var external = await api.GetExternalBankAccountsAsync();
            ExternalAccounts.Clear();
            foreach (var account in external) ExternalAccounts.Add(account);
            ExternalBankStatus.Text = external.Count == 0 ? "No accounts were shared by the sandbox bank." : "Connected sandbox data · refreshed now";
        }
        catch (Exception exception)
        {
            ExternalAccounts.Clear();
            ExternalBankStatus.Text = exception.Message;
        }
        finally
        {
            isLoading = false;
        }
    }

    private async void OnCreateAccountClicked(object? sender, EventArgs e)
    {
        var name = HolderNameEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlertAsync("Account name needed", "Enter a name for this demo account.", "OK");
            return;
        }

        try
        {
            await api.CreateAccountAsync(name, 0m, CurrencyPicker.SelectedItem?.ToString() ?? "GBP");
            HolderNameEntry.Text = "";
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("Could not create account", exception.Message, "OK");
        }
    }

    private async void OnConnectBankClicked(object? sender, EventArgs e)
    {
        try
        {
            var (authorizationUrl, state) = await api.BeginExternalBankConnectionAsync();
            pendingState = state;
            await Launcher.Default.OpenAsync(new Uri(authorizationUrl));
            await DisplayAlertAsync("Sandbox consent", "Complete the test-bank consent in your browser. On the return page, copy its authorization code and paste it in Accounts. StudentBank never asks for your bank password.", "OK");
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("Could not start connection", exception.Message, "OK");
        }
    }

    private async void OnFinishConnectClicked(object? sender, EventArgs e)
    {
        var code = AuthorizationCodeEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(pendingState) || string.IsNullOrWhiteSpace(code))
        {
            await DisplayAlertAsync("Authorization needed", "Start the connection, approve the sandbox request, then paste the returned code.", "OK");
            return;
        }

        try
        {
            await api.CompleteExternalBankConnectionAsync(pendingState, code);
            pendingState = "";
            AuthorizationCodeEntry.Text = "";
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            await DisplayAlertAsync("Could not finish connection", exception.Message, "OK");
        }
    }
}
