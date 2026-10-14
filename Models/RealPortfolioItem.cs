namespace HalalStocks.Models;

// One purchase lot. The same stock may appear on several rows — each row is an
// independent buy with its own date, quantity and price.
public class RealPortfolioItem
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    public RealPortfolio Portfolio { get; set; } = null!;

    public int StockId { get; set; }
    public Stock Stock { get; set; } = null!;

    public DateOnly PurchaseDate { get; set; }
    public int Quantity { get; set; }
    public decimal PurchasePrice { get; set; }

    // Purchase-side derived values are persisted (mirrors WalletItem); market-side values
    // are recomputed on every render so they can never go stale in the DB.
    public decimal TotalPurchase { get; set; }
    public decimal PurchaseWeightPercent { get; set; }

    // Last price successfully fetched from TradingView — used only as a fallback when the
    // live fetch fails, so the table still renders (flagged as stale).
    public decimal? LastMarketPrice { get; set; }
    public DateTime? LastMarketPriceAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
