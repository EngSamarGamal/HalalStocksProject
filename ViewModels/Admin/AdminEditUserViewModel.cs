using HalalStocks.Models;
using System.ComponentModel.DataAnnotations;

namespace HalalStocks.ViewModels.Admin;

public class AdminEditUserViewModel
{
    public string Id { get; set; } = "";
    public bool IsPublicUser { get; set; }
    public string UserName { get; set; } = "";  // read-only display

    // Admin user fields
    [Display(Name = "الاسم الكامل")]
    public string FullName { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.Editor;

    // Public user fields
    [Display(Name = "الاسم الأول")]
    public string FirstName { get; set; } = "";

    [Display(Name = "الاسم الأخير")]
    public string LastName { get; set; } = "";

    [Display(Name = "رقم الهاتف")]
    public string PhoneNumber { get; set; } = "";

    [Display(Name = "مهتم بالنسخة المدفوعة")]
    public bool InterestedInPaidVersion { get; set; }

    // Both
    [Required(ErrorMessage = "البريد الإلكتروني مطلوب")]
    [EmailAddress(ErrorMessage = "بريد إلكتروني غير صالح")]
    [Display(Name = "البريد الإلكتروني")]
    public string Email { get; set; } = "";

    // Read-only display of current state — actual toggling posts to a dedicated action
    // (UsersController.ToggleAnalyst), same shape as the Role quick-edit on the list page,
    // since IsAnalyst is orthogonal to Role and applies to both admin and public users.
    public bool IsAnalyst { get; set; }
}
