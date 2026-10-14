namespace HalalStocks.Models;

public class SiteSettings
{
    public int Id { get; set; }
    public string? WhatsAppNumber { get; set; }
    public string? WhatsAppMessage { get; set; }
    public string? FacebookUrl { get; set; }
    public string? TelegramUrl { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
