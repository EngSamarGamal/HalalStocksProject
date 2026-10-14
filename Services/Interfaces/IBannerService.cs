using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

public interface IBannerService
{
    // Homepage slider — only active banners, ordered for display.
    Task<IEnumerable<Banner>> GetActiveOrderedAsync();

    Task<IEnumerable<Banner>> GetAllAsync();
    Task<Banner?> GetByIdAsync(int id);
    Task<Banner> CreateAsync(Banner banner);
    Task<Banner> UpdateAsync(Banner banner);
    Task DeleteAsync(int id);
    Task ToggleActiveAsync(int id);
}
