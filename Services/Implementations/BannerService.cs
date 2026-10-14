using HalalStocks.Data;
using HalalStocks.Models;
using Microsoft.EntityFrameworkCore;
using HalalStocks.Services.Interfaces;

namespace HalalStocks.Services.Implementations;

public class BannerService : IBannerService
{
    private readonly AppDbContext _db;
    public BannerService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Banner>> GetActiveOrderedAsync() =>
        await _db.Banners.Where(b => b.IsActive)
            .OrderBy(b => b.DisplayOrder).ThenByDescending(b => b.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<Banner>> GetAllAsync() =>
        await _db.Banners.OrderBy(b => b.DisplayOrder).ThenByDescending(b => b.CreatedAt).ToListAsync();

    public async Task<Banner?> GetByIdAsync(int id) =>
        await _db.Banners.FindAsync(id);

    public async Task<Banner> CreateAsync(Banner banner)
    {
        banner.CreatedAt = DateTime.UtcNow;
        banner.UpdatedAt = DateTime.UtcNow;
        _db.Banners.Add(banner);
        await _db.SaveChangesAsync();
        return banner;
    }

    public async Task<Banner> UpdateAsync(Banner banner)
    {
        banner.UpdatedAt = DateTime.UtcNow;
        _db.Banners.Update(banner);
        await _db.SaveChangesAsync();
        return banner;
    }

    public async Task DeleteAsync(int id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner != null) { _db.Banners.Remove(banner); await _db.SaveChangesAsync(); }
    }

    public async Task ToggleActiveAsync(int id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null) return;
        banner.IsActive = !banner.IsActive;
        banner.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }
}
