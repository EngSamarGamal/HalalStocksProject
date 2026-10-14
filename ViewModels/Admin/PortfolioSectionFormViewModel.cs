using System.ComponentModel.DataAnnotations;

namespace HalalStocks.ViewModels.Admin;

public class PortfolioSectionFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "العنوان مطلوب")]
    public string Title { get; set; } = string.Empty;

    public string? Content { get; set; }

    public IFormFile? Image { get; set; }

    public string? ExistingImagePath { get; set; }

    public int DisplayOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}
