using System.ComponentModel.DataAnnotations;
using HalalStocks.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HalalStocks.ViewModels.Admin;

public class ArticleFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "الرجاء إدخال العنوان")]
    [Display(Name = "العنوان")]
    public string TitleAr { get; set; } = string.Empty;

    [Required(ErrorMessage = "الرجاء إدخال المحتوى")]
    [Display(Name = "المحتوى")]
    public string ContentAr { get; set; } = string.Empty;

    // Nullable so an unselected dropdown ("") binds to null and triggers the [Required]
    // message below, instead of ASP.NET Core's generic "the value '' is invalid" type-
    // conversion error that a non-nullable int would produce for an empty posted value.
    // Ignored server-side when Type == Analytical — that type auto-assigns its category.
    [Display(Name = "التصنيف")]
    public int? ArticleCategoryId { get; set; }

    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();

    [Display(Name = "نوع المقال")]
    public ArticleType Type { get; set; } = ArticleType.Standard;

    public IEnumerable<SelectListItem> ArticleTypes { get; set; } = new List<SelectListItem>();

    public string? ExistingImagePath { get; set; }
    public IFormFile? FeaturedImage { get; set; }
    public bool IsPublished { get; set; }
}
