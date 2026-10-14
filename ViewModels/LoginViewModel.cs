using System.ComponentModel.DataAnnotations;

namespace HalalStocks.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "اسم المستخدم مطلوب")]
    [Display(Name = "اسم المستخدم")]
    public string UserName { get; set; } = "";

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [DataType(DataType.Password)]
    [Display(Name = "كلمة المرور")]
    public string Password { get; set; } = "";

    [Display(Name = "تذكرني")]
    public bool RememberMe { get; set; } = false;
}
