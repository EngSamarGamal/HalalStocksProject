using System.ComponentModel.DataAnnotations;

namespace HalalStocks.ViewModels.Admin;

public class BannerFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "الرجاء إدخال العنوان")]
    [Display(Name = "العنوان")]
    public string TitleAr { get; set; } = string.Empty;

    [Display(Name = "الوصف")]
    public string? DescriptionAr { get; set; }

    [Display(Name = "اسم الزر")]
    [StringLength(100)]
    public string? ButtonText { get; set; }

    [Display(Name = "رابط الزر")]
    [StringLength(500)]
    [RegularExpression(@"^(https?://|/).*", ErrorMessage = "الرابط يجب أن يبدأ بـ http:// أو https:// أو /")]
    public string? ButtonUrl { get; set; }

    public string? ExistingImagePath { get; set; }
    public IFormFile? Image { get; set; }

    [Display(Name = "ترتيب الظهور")]
    public int DisplayOrder { get; set; }

    [Display(Name = "مفعّل")]
    public bool IsActive { get; set; } = true;
}
