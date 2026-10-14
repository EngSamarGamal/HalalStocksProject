using System.ComponentModel.DataAnnotations;

namespace HalalStocks.ViewModels;

public class ProfileViewModel
{
    public string UserName { get; set; } = "";   // read-only display

    [Required(ErrorMessage = "الاسم الأول مطلوب")]
    [Display(Name = "الاسم الأول")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "الاسم الأخير مطلوب")]
    [Display(Name = "الاسم الأخير")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
    [Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "رقم الهاتف مطلوب")]
    [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
    [Display(Name = "رقم الهاتف")]
    public string PhoneNumber { get; set; } = "";

    [Display(Name = "أرغب في الاشتراك في النسخة المدفوعة")]
    public bool InterestedInPaidVersion { get; set; }

    public bool IsAnalyst { get; set; }
}
