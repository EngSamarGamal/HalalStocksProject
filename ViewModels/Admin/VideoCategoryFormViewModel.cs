using System.ComponentModel.DataAnnotations;

namespace HalalStocks.ViewModels.Admin;

public class VideoCategoryFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "الرجاء إدخال اسم الفئة")]
    [Display(Name = "الاسم")]
    public string Name { get; set; } = string.Empty;
}
