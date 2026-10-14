namespace HalalStocks.ViewModels;

// One purchase lot as rendered in the المحفظة الفعلية table. Market-side values are computed
// per request from the current price rather than read back from the database.
public class RealPortfolioRowViewModel
{
    public int Id { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public string StockTicker { get; set; } = string.Empty;
    public string StockNameAr { get; set; } = string.Empty;
    public string SectorNameAr { get; set; } = string.Empty;
    public string ShariahLabel { get; set; } = string.Empty;
    public string ShariahBadgeClass { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal TotalPurchase { get; set; }
    public decimal PurchaseWeightPercent { get; set; }

    public decimal? MarketPrice { get; set; }
    public DateTime? MarketPriceAt { get; set; }
    public bool IsPriceStale { get; set; }

    public decimal MarketValue => (MarketPrice ?? 0) * Quantity;
    public decimal ProfitLoss => MarketPrice == null ? 0 : MarketValue - TotalPurchase;
    public decimal ProfitLossPerShare => MarketPrice == null ? 0 : MarketPrice.Value - PurchasePrice;
    public decimal ProfitLossPercent => MarketPrice == null || TotalPurchase == 0
        ? 0
        : ProfitLoss / TotalPurchase * 100m;
}
