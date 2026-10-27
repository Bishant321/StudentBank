using System.Collections.ObjectModel;
using MyApp.Models;

namespace MyApp;

public partial class MainPage : ContentPage
{
    private bool balanceVisible = true;

    public ObservableCollection<BankTransactionDisplay> Transactions { get; } = new()
    {
        new("Campus Cafe", "Today · Food and drink", "−£8.50", "☕", "#263A34"),
        new("Part-time pay", "Yesterday · Income", "+£320.00", "↙", "#08765B"),
        new("City Bus", "Mon · Travel", "−£4.80", "▣", "#263A34"),
        new("Bookshop", "Sun · Study", "−£22.40", "▤", "#263A34")
    };

    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    private void OnToggleBalanceClicked(object? sender, EventArgs e)
    {
        balanceVisible = !balanceVisible;
        BalanceLabel.Text = balanceVisible ? "£2,450.75" : "••••••••";
        if (sender is Button button)
            button.Text = balanceVisible ? "Hide" : "Show";
    }

    private async void OnActionClicked(object? sender, EventArgs e)
    {
        var action = (sender as Button)?.CommandParameter?.ToString() ?? "This action";
        await DisplayAlertAsync(action, "This is a coursework demo using sample data. No real money moves in this prototype.", "OK");
    }

    private async void OnAssistantClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//Assistant");

    private async void OnAccountsClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("//Accounts");
}
