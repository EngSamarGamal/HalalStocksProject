using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

public interface ISiteSettingsService
{
    Task<SiteSettings> GetSettingsAsync();
    Task SaveSettingsAsync(SiteSettings settings);
}
