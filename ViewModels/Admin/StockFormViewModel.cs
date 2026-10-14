using HalalStocks.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HalalStocks.ViewModels.Admin;

public class StockFormViewModel
{
    public int Id { get; set; }
    public string TickerSymbol { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public int CategoryId { get; set; }
    public string Exchange { get; set; } = "EGX";
    public ShariahStatus ShariahStatus { get; set; }
    public decimal? PurificationRatio { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    // Shariah Screening Ratios
    public decimal? ProhibitedIncomeRatio { get; set; }
    public decimal? NonShariahInvestmentRatio { get; set; }
    public decimal? NonShariahObligationsRatio { get; set; }
    public decimal? LiquidAssetsRatio { get; set; }
    public DateTime? LastReviewedAt { get; set; }
    public string? AdditionalNotes { get; set; }

    // Company financials
    [Range(0, long.MaxValue, ErrorMessage = "إجمالي أسهم الشركة يجب أن يكون رقماً صحيحاً موجباً")]
    public long? TotalCompanyShares { get; set; }

    [Range(0, 9999999999999999.99, ErrorMessage = "إجمالي أصول الشركة يجب أن يكون رقماً موجباً")]
    public decimal? TotalCompanyAssets { get; set; }

    // Purity grades (multi-select)
    public List<int> SelectedPurityGrades { get; set; } = new();

    // Index selection
    public List<int> SelectedIndexIds { get; set; } = new();
    public IEnumerable<SelectListItem> AvailableIndices { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
}
