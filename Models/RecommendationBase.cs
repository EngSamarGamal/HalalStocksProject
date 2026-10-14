namespace HalalStocks.Models;

public abstract class RecommendationBase
{
    public int Id { get; set; }

    public int StockId { get; set; }
    public Stock Stock { get; set; } = null!;

    public string PurityGradeLabel { get; set; } = string.Empty;
    public string GeneralTrend { get; set; } = string.Empty;
    public decimal ClosingPrice { get; set; }
    public decimal Support1 { get; set; }
    public decimal Support2 { get; set; }
    public decimal Resistance1 { get; set; }
    public decimal Resistance2 { get; set; }
    public decimal StopLoss { get; set; }
    public decimal YtdChangePercent { get; set; }
    public int RecommendationTypeId { get; set; }
    public RecommendationType RecommendationType { get; set; } = null!;
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
