using System.ComponentModel.DataAnnotations;

namespace HalalStocks.ViewModels;

public class RealPortfolioItemFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "يرجى اختيار تاريخ الشراء")]
    [Display(Name = "تاريخ الشراء")]
    [DataType(DataType.Date)]
    public DateOnly? PurchaseDate { get; set; }

    [Required(ErrorMessage = "يرجى اختيار السهم")]
    [Display(Name = "اسم السهم")]
    public int? StockId { get; set; }

    [Required(ErrorMessage = "يرجى إدخال عدد الأسهم")]
    [Range(1, int.MaxValue, ErrorMessage = "عدد الأسهم يجب أن يكون 1 على الأقل")]
    [Display(Name = "عدد الأسهم")]
    public int? Quantity { get; set; }

    [Required(ErrorMessage = "يرجى إدخال سعر الشراء")]
    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "سعر الشراء يجب أن يكون أكبر من صفر")]
    [Display(Name = "سعر الشراء")]
    public decimal? PurchasePrice { get; set; }

    // Display-only context for the selected stock, so the form re-renders intact
    // after a server-side validation failure without a second lookup in the view.
    public string? SelectedStockTicker { get; set; }
    public string? SelectedStockNameAr { get; set; }
    public string? SelectedSectorNameAr { get; set; }
    public string? SelectedShariahLabel { get; set; }
    public decimal? SelectedMarketPrice { get; set; }
}
