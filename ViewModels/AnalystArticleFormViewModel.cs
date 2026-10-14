using System.ComponentModel.DataAnnotations;
using HalalStocks.Models;

namespace HalalStocks.ViewModels;

public class AnalystArticleFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "الرجاء إدخال العنوان")]
    [Display(Name = "العنوان")]
    public string TitleAr { get; set; } = string.Empty;

    [Required(ErrorMessage = "الرجاء إدخال المحتوى")]
    [Display(Name = "المحتوى")]
    public string ContentAr { get; set; } = string.Empty;

    public string? ExistingImagePath { get; set; }
    public IFormFile? FeaturedImage { get; set; }

    // Display-only on Edit — populated from the loaded article, never bound from the form.
    public ArticleApprovalStatus ApprovalStatus { get; set; }
    public string? HideReason { get; set; }
}
