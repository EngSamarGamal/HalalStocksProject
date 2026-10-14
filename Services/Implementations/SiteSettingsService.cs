using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Services.Implementations;

public class SiteSettingsService : ISiteSettingsService
{
    private readonly AppDbContext _db;

    public SiteSettingsService(AppDbContext db) => _db = db;

    public async Task<SiteSettings> GetSettingsAsync()
    {
        return await _db.SiteSettings.FirstOrDefaultAsync()
               ?? new SiteSettings();
    }

    public async Task SaveSettingsAsync(SiteSettings settings)
    {
        var existing = await _db.SiteSettings.FirstOrDefaultAsync();
        if (existing == null) return;

        existing.WhatsAppNumber  = settings.WhatsAppNumber;
        existing.WhatsAppMessage = settings.WhatsAppMessage;
        existing.FacebookUrl     = settings.FacebookUrl;
        existing.TelegramUrl     = settings.TelegramUrl;
        existing.UpdatedAt       = DateTime.UtcNow;

        await _db.SaveChangesAsync();
    }
}
