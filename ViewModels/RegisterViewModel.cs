using System.ComponentModel.DataAnnotations;

namespace HalalStocks.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "الاسم الأول مطلوب")]
    [Display(Name = "الاسم الأول")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "الاسم الأخير مطلوب")]
    [Display(Name = "الاسم الأخير")]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "اسم المستخدم مطلوب")]
    [Display(Name = "اسم المستخدم")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صحيح")]
    [Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "رقم الهاتف مطلوب")]
    [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
    [Display(Name = "رقم الهاتف")]
    public string PhoneNumber { get; set; } = "";

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [MinLength(8, ErrorMessage = "كلمة المرور يجب أن تكون 8 أحرف على الأقل")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "تأكيد كلمة المرور مطلوب")]
    [Compare("Password", ErrorMessage = "كلمتا المرور غير متطابقتين")]
    [DataType(DataType.Password)]
    [Display(Name = "تأكيد كلمة المرور")]
    public string ConfirmPassword { get; set; } = "";

    [Display(Name = "أرغب في الاشتراك في النسخة المدفوعة")]
    public bool InterestedInPaidVersion { get; set; } = false;
}
