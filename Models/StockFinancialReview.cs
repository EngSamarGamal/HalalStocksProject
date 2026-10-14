namespace HalalStocks.Models;

public class StockFinancialReview
{
    public int Id { get; set; }

    public int StockId { get; set; }
    public Stock Stock { get; set; } = null!;
    public int FinancialStatementTypeId { get; set; }
    public FinancialStatementType StatementType { get; set; } = null!;

    public DateTime ReviewDate { get; set; }
    public int ReviewYear { get; set; }

    public decimal? ProhibitedIncomeRatio { get; set; }
    public decimal? NonShariahInvestmentRatio { get; set; }
    public decimal? NonShariahObligationsRatio { get; set; }
    public decimal? LiquidAssetsRatio { get; set; }

    public ShariahStatus ShariahStatus { get; set; }

    public PurityGrade PurityGrades { get; set; } = PurityGrade.None;

    public string? Notes { get; set; }

    public string? PdfUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
