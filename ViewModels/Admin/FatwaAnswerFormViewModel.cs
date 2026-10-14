using System.ComponentModel.DataAnnotations;
using HalalStocks.Models;

namespace HalalStocks.ViewModels.Admin;

public class FatwaAnswerFormViewModel
{
    public int Id { get; set; }

    // Read-only context, re-filled from the DB on every render — never bound from the form.
    public string Question { get; set; } = string.Empty;
    public string AskerName { get; set; } = string.Empty;
    public string? AskerEmail { get; set; }
    public string? AskerPhone { get; set; }
    public DateTime CreatedAt { get; set; }
    public FatwaStatus Status { get; set; }

    [StringLength(20000, ErrorMessage = "الإجابة طويلة جداً (الحد الأقصى 20000 حرف)")]
    public string? AnswerHtml { get; set; }
    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }
}
