using System.ComponentModel.DataAnnotations;
using HalalStocks.Models;

namespace HalalStocks.ViewModels;

public class WalletItemFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "يرجى اختيار السهم")]
    [Display(Name = "الكود / السهم")]
    public int? StockId { get; set; }

    [Required(ErrorMessage = "يرجى إدخال عدد الأسهم")]
    [Display(Name = "عدد الأسهم")]
    public int? Quantity { get; set; }

    [Required(ErrorMessage = "يرجى إدخال سعر الشراء")]
    [Display(Name = "سعر الشراء")]
    public decimal? PurchasePrice { get; set; }

    [Required(ErrorMessage = "يرجى إدخال المستهدف")]
    [Display(Name = "المستهدف")]
    public decimal? TargetPrice { get; set; }

    [Required(ErrorMessage = "يرجى إدخال وقف الخسارة")]
    [Display(Name = "وقف الخسارة")]
    public decimal? StopLossPrice { get; set; }

    [Display(Name = "السيولة بالمحفظة")]
    public decimal? CashBalance { get; set; }

    public string PurchasePriceSource { get; set; } = "ClosePrice";
    public string TargetPriceSource { get; set; } = "Resistance1";
    public string StopLossSource { get; set; } = "Support1";

    public List<RecommendationBase> RecommendationStocks { get; set; } = new();
    public InvestmentWallet? Wallet { get; set; }
    public decimal? CurrentAvailableCash { get; set; }
    public decimal? CurrentTotalPurchase { get; set; }
}
