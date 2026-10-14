using System.ComponentModel.DataAnnotations;

namespace HalalStocks.ViewModels;

public class WalletRiskFormViewModel
{
    [Required(ErrorMessage = "يرجى إدخال نسبة المخاطر")]
    [Range(0, 100, ErrorMessage = "يجب أن تكون نسبة المخاطر بين 0 و 100")]
    [Display(Name = "ادخل نسبة المخاطر")]
    public decimal? RiskPercent { get; set; }
}
