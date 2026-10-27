namespace MyApp.Models;

public sealed record BankTransactionDisplay(
    string Title,
    string Subtitle,
    string Amount,
    string Icon,
    string AmountColor);
