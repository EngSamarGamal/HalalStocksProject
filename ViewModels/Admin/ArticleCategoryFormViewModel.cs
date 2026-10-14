using System.ComponentModel.DataAnnotations;

namespace HalalStocks.ViewModels.Admin;

public class ArticleCategoryFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "الرجاء إدخال اسم التصنيف")]
    [Display(Name = "الاسم")]
    public string NameAr { get; set; } = string.Empty;
}
