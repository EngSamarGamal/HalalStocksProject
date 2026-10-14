namespace HalalStocks.ViewModels;

public class RealPortfolioIndexViewModel
{
    public RecommendationsPageState PageState { get; set; } = RecommendationsPageState.List;

    public bool HasPortfolio { get; set; }
    public decimal Capital { get; set; }
    public decimal InvestedCapital { get; set; }
    public decimal AvailableCash => Capital - InvestedCapital;

    public List<RealPortfolioRowViewModel> Rows { get; set; } = new();

    public RealPortfolioItemFormViewModel Form { get; set; } = new();

    /// <summary>True when the live price fetch failed entirely — the table falls back to stored prices.</summary>
    public bool PriceFetchFailed { get; set; }

    public decimal TotalMarketValue => Rows.Sum(r => r.MarketValue);
    public decimal TotalProfitLoss => Rows.Sum(r => r.ProfitLoss);
    public decimal TotalProfitLossPerShare => Rows.Sum(r => r.ProfitLossPerShare);
    public decimal TotalPurchaseWeightPercent => Rows.Sum(r => r.PurchaseWeightPercent);

    // صافي الربح/الخسارة as a % of the amount actually invested, not of total capital.
    public decimal NetProfitLossPercent => InvestedCapital == 0 ? 0 : TotalProfitLoss / InvestedCapital * 100m;

    public bool IsEditing => Form.Id.HasValue;

    /// <summary>
    /// Route prefix every form action and link on the page posts to. Defaults to the user's own
    /// page; the admin wallet-management mode points it at /admin/wallet-management/{userId}/portfolio
    /// so the same view can be rendered on behalf of another user.
    /// </summary>
    public string BasePath { get; set; } = "/recommendations/portfolio";

    /// <summary>True when an admin is managing another user's portfolio (swaps layout and nav tabs).</summary>
    public bool IsManagementMode { get; set; }
}
