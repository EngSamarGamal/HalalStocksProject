using System.ComponentModel.DataAnnotations;
using HalalStocks.Models;

namespace HalalStocks.ViewModels;

public class StockRecommendationFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "يرجى اختيار السهم")]
    [Display(Name = "السهم")]
    public int StockId { get; set; }

    [Required(ErrorMessage = "يرجى إدخال درجة النقاء")]
    [Display(Name = "درجة النقاء")]
    public string PurityGradeLabel { get; set; } = string.Empty;

    [Display(Name = "الاتجاه العام")]
    public string? GeneralTrend { get; set; }

    [Display(Name = "الإغلاق")]
    [Required(ErrorMessage = "يرجى إدخال الإغلاق")]
    public decimal? ClosingPrice { get; set; }

    [Display(Name = "دعم 1")]
    [Required(ErrorMessage = "يرجى إدخال دعم 1")]
    public decimal? Support1 { get; set; }

    [Display(Name = "دعم 2")]
    [Required(ErrorMessage = "يرجى إدخال دعم 2")]
    public decimal? Support2 { get; set; }

    [Display(Name = "مقاومة 1")]
    [Required(ErrorMessage = "يرجى إدخال مقاومة 1")]
    public decimal? Resistance1 { get; set; }

    [Display(Name = "مقاومة 2")]
    [Required(ErrorMessage = "يرجى إدخال مقاومة 2")]
    public decimal? Resistance2 { get; set; }

    [Display(Name = "وقف الخسارة")]
    [Required(ErrorMessage = "يرجى إدخال وقف الخسارة")]
    public decimal? StopLoss { get; set; }

    [Display(Name = "التغير % من بداية السنة")]
    [Required(ErrorMessage = "يرجى إدخال التغير %")]
    public decimal? YtdChangePercent { get; set; }

    [Display(Name = "التوصية")]
    [Required(ErrorMessage = "يرجى اختيار نوع التوصية")]
    public int RecommendationTypeId { get; set; }

    [Display(Name = "ملاحظات")]
    public string? Notes { get; set; }

    [Display(Name = "حالة الظهور")]
    public bool IsHidden { get; set; }

    // True when the form is rendering a private (user) recommendation. Drives
    // conditional field rendering in _FormFields.cshtml (hide YtdChangePercent,
    // make GeneralTrend optional). Set by MyCreate.cshtml / MyEdit.cshtml.
    public bool IsPrivate { get; set; }

    public List<RecommendationType> RecommendationTypes { get; set; } = new();
}
