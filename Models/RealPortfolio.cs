namespace HalalStocks.Models;

// The user's real-money portfolio (المحفظة الفعلية). Deliberately separate from
// InvestmentWallet, which is a paper/simulation portfolio with its own liquidity and risk cap.
public class RealPortfolio
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    // رأس المال — total capital the user has allocated to this portfolio.
    public decimal Capital { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RealPortfolioItem> Items { get; set; } = new List<RealPortfolioItem>();
}
