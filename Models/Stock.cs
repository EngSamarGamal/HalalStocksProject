namespace HalalStocks.Models;

public enum ShariahStatus { Halal, MustPurify, Prohibited, NotReviewed, AwaitingFinancialStatements, StatusBlocked }

[Flags]
public enum PurityGrade
{
    None                        = 0,
    Pure                        = 1,   // نقي
    SemiPure                    = 2,   // شبه نقي
    NonShariahProhibitedIncome  = 4,   // غير شرعي دخل محظور
    NonShariahDebts             = 8,   // غير شرعي قروض
    NonShariahRibaInvestments   = 16,  // غير شرعي استثمارات ربوية
    ShariaMixedA                = 128, // شرعي مختلط A
    ShariaMixedB                = 32,  // شرعي مختلط B
    ShariaMixedC                = 256, // شرعي مختلط C
    NonShariahActivity          = 64   // غير شرعي نشاط
}

public class Stock
{
    public int Id { get; set; }
    public string TickerSymbol { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public int CategoryId { get; set; }
    public StockCategory Category { get; set; } = null!;
    public string Exchange { get; set; } = "EGX";
    public ShariahStatus ShariahStatus { get; set; }
    public decimal? PurificationRatio { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Shariah Screening Ratios
    public decimal? ProhibitedIncomeRatio { get; set; }
    public decimal? NonShariahInvestmentRatio { get; set; }
    public decimal? NonShariahObligationsRatio { get; set; }
    public decimal? LiquidAssetsRatio { get; set; }

    // Company financials (used by the capital purification calculator)
    public long?    TotalCompanyShares { get; set; }
    public decimal? TotalCompanyAssets { get; set; }

    // Audit
    public DateTime? LastReviewedAt { get; set; }
    public string? AdditionalNotes { get; set; }

    // Purity grades (flags – multiple values allowed)
    public PurityGrade PurityGrades { get; set; } = PurityGrade.None;

    // Index membership
    public ICollection<StockIndex> Indices { get; set; } = new List<StockIndex>();

    // Financial reviews
    public ICollection<StockFinancialReview> FinancialReviews { get; set; } = new List<StockFinancialReview>();
}
