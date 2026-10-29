namespace MyApp.Models;

public sealed class BankingAccount
{
    public string AccountNumber { get; set; } = string.Empty;
    public string HolderName { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "GBP";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string DisplayName => $"{HolderName} · {Currency} {Balance:N2} · •• {AccountNumber[^Math.Min(4, AccountNumber.Length)..]}";
}

public sealed class AccountStatement
{
    public string AccountNumber { get; set; } = string.Empty;
    public string Currency { get; set; } = "GBP";
    public decimal CurrentBalance { get; set; }
    public List<StatementTransaction> Transactions { get; set; } = new();
}

public sealed class StatementTransaction
{
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public sealed class ExternalBankAccount
{
    public string Provider { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = "GBP";
    public decimal Balance { get; set; }
    public List<ExternalBankTransaction> Transactions { get; set; } = new();
    public string RecentActivity => Transactions.Count == 0
        ? "No recent transactions"
        : string.Join("\n", Transactions.Take(3).Select(item => $"{item.Timestamp:dd MMM} · {item.Description} · {item.Currency} {item.Amount:N2}"));
}

public sealed class ExternalBankTransaction
{
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "Other";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GBP";
    public DateTimeOffset Timestamp { get; set; }
}

public sealed class MarketQuote
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Change { get; set; }
    public string ChangePercent { get; set; } = string.Empty;
    public string TradingDay { get; set; } = string.Empty;
    public string DataNotice { get; set; } = string.Empty;
}
