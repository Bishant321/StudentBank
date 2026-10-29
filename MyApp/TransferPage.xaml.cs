using Microsoft.Extensions.DependencyInjection;
using MyApp.Models;
using MyApp.Services;

namespace MyApp;

public partial class TransferPage : ContentPage
{
    private readonly BankingApiClient api = App.ServiceProvider.GetRequiredService<BankingApiClient>();
    private List<BankingAccount> accounts = new();

    public TransferPage() => InitializeComponent();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            accounts = await api.GetAccountsAsync();
            FromPicker.ItemsSource = accounts;
            if (accounts.Count > 0) FromPicker.SelectedIndex = 0;
        }
        catch (Exception exception)
        {
            StatusLabel.Text = exception.Message;
            StatusLabel.IsVisible = true;
        }
    }

    private void OnFromChanged(object? sender, EventArgs e)
    {
        if (FromPicker.SelectedItem is not BankingAccount source) return;
        ToPicker.ItemsSource = accounts
            .Where(account => account.AccountNumber != source.AccountNumber && account.Currency == source.Currency)
            .ToList();
        ToPicker.SelectedIndex = ToPicker.Items.Count > 0 ? 0 : -1;
    }

    private async void OnReviewTransferClicked(object? sender, EventArgs e)
    {
        StatusLabel.IsVisible = false;
        if (FromPicker.SelectedItem is not BankingAccount source || ToPicker.SelectedItem is not BankingAccount destination)
        {
            StatusLabel.Text = "Create or choose two different accounts in the same currency first.";
            StatusLabel.IsVisible = true;
            return;
        }
        if (!decimal.TryParse(AmountEntry.Text, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.CurrentCulture, out var amount) || amount <= 0m)
        {
            StatusLabel.Text = "Enter an amount greater than zero.";
            StatusLabel.IsVisible = true;
            return;
        }
        if (amount > source.Balance)
        {
            StatusLabel.Text = "The amount is greater than the available demo balance.";
            StatusLabel.IsVisible = true;
            return;
        }

        var confirmed = await DisplayAlertAsync("Confirm demo transfer",
            $"Move {source.Currency} {amount:N2} from {source.HolderName} to {destination.HolderName}?\n\nThis changes only the StudentBank coursework database.",
            "Confirm", "Cancel");
        if (!confirmed) return;

        try
        {
            await api.TransferAsync(source.AccountNumber, destination.AccountNumber, amount, DescriptionEntry.Text?.Trim() ?? "Transfer");
            await DisplayAlertAsync("Transfer recorded", "The demo account balances and transaction records have been updated.", "OK");
            AmountEntry.Text = "";
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception exception)
        {
            StatusLabel.Text = exception.Message;
            StatusLabel.IsVisible = true;
        }
    }
}
