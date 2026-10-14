namespace HalalStocks.Models;

public class InvestmentWallet
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
    public decimal CashBalance { get; set; }

    // Maximum total risk % the user allows their portfolio to carry (0-100).
    // Null = not set yet → adding stocks is blocked until a value is saved.
    public decimal? MaxRiskPercent { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<WalletItem> Items { get; set; } = new List<WalletItem>();
}
