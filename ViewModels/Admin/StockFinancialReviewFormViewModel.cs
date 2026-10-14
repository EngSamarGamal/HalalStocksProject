using System.ComponentModel.DataAnnotations;
using HalalStocks.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HalalStocks.ViewModels.Admin;

public class StockFinancialReviewFormViewModel
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public string StockName { get; set; } = string.Empty;

    [Required(ErrorMessage = "تاريخ المراجعة مطلوب")]
    public DateTime ReviewDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "السنة المالية مطلوبة")]
    [Range(2000, 2100, ErrorMessage = "السنة غير صالحة")]
    public int ReviewYear { get; set; } = DateTime.Today.Year;

    [Required(ErrorMessage = "نوع القوائم المالية مطلوب")]
    public int FinancialStatementTypeId { get; set; }

    [Range(0, 100, ErrorMessage = "يجب أن تكون النسبة بين 0 و 100")]
    public decimal? ProhibitedIncomeRatio { get; set; }

    [Range(0, 100, ErrorMessage = "يجب أن تكون النسبة بين 0 و 100")]
    public decimal? NonShariahInvestmentRatio { get; set; }

    [Range(0, 100, ErrorMessage = "يجب أن تكون النسبة بين 0 و 100")]
    public decimal? NonShariahObligationsRatio { get; set; }

    [Range(0, 100, ErrorMessage = "يجب أن تكون النسبة بين 0 و 100")]
    public decimal? LiquidAssetsRatio { get; set; }

    [Required(ErrorMessage = "موقف السهم مطلوب")]
    public ShariahStatus ShariahStatus { get; set; }

    public List<int> SelectedPurityGrades { get; set; } = new();

    public string? Notes { get; set; }

    public IFormFile? PdfFile { get; set; }
    public string? ExistingPdfUrl { get; set; }

    public IEnumerable<SelectListItem> StatementTypes { get; set; } = Enumerable.Empty<SelectListItem>();
}
