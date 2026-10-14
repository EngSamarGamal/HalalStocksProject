namespace HalalStocks.Models;

public class Banner
{
    public int Id { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string ImagePath { get; set; } = string.Empty;
    public string? ButtonText { get; set; }
    public string? ButtonUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
