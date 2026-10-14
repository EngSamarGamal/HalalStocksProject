using System.ComponentModel.DataAnnotations;

namespace HalalStocks.ViewModels;

public class AnalystProfileFormViewModel
{
    [Display(Name = "نبذة تعريفية")]
    public string? SummaryAr { get; set; }

    public string? ExistingPhotoPath { get; set; }
    public IFormFile? Photo { get; set; }
}
