using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HalalStocks.ViewModels.Admin;

public class VideoFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "العنوان")]
    public string? Title { get; set; }

    [Display(Name = "الوصف")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "الرجاء إدخال رابط يوتيوب")]
    [Display(Name = "رابط يوتيوب")]
    public string YouTubeUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "الرجاء اختيار الفئة")]
    [Display(Name = "الفئة")]
    public int CategoryId { get; set; }

    public string? ThumbnailUrl { get; set; }

    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
}
