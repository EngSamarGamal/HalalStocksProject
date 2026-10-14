using System.ComponentModel.DataAnnotations;

namespace HalalStocks.ViewModels;

public class FatwaAskViewModel
{
    [Required(ErrorMessage = "الرجاء كتابة سؤالك")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "يجب أن يتراوح السؤال بين 10 و2000 حرف")]
    public string Question { get; set; } = string.Empty;
}
