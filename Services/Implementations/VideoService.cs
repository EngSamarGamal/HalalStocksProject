using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Services.Implementations;

public class VideoService : IVideoService
{
    private readonly AppDbContext _db;
    public VideoService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<Video>> GetLatestAsync(int count) =>
        await _db.Videos.Include(v => v.Category)
            .OrderByDescending(v => v.CreatedAt)
            .Take(count).ToListAsync();

    public async Task<(IEnumerable<Video> Items, int TotalCount)> GetPublicAsync(string? search, int? categoryId, int page, int pageSize)
    {
        var query = _db.Videos.Include(v => v.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(v => v.Title.Contains(search) ||
                (v.Description != null && v.Description.Contains(search)));

        if (categoryId.HasValue)
            query = query.Where(v => v.CategoryId == categoryId.Value);

        query = query.OrderByDescending(v => v.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<(IEnumerable<Video> Items, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        var query = _db.Videos.Include(v => v.Category).OrderByDescending(v => v.CreatedAt);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<Video?> GetByIdAsync(int id) =>
        await _db.Videos.Include(v => v.Category).FirstOrDefaultAsync(v => v.Id == id);

    public async Task<Video> CreateAsync(Video video)
    {
        video.CreatedAt = DateTime.UtcNow;
        video.UpdatedAt = DateTime.UtcNow;
        _db.Videos.Add(video);
        await _db.SaveChangesAsync();
        return video;
    }

    public async Task<Video> UpdateAsync(Video video)
    {
        video.UpdatedAt = DateTime.UtcNow;
        _db.Videos.Update(video);
        await _db.SaveChangesAsync();
        return video;
    }

    public async Task DeleteAsync(int id)
    {
        var video = await _db.Videos.FindAsync(id);
        if (video != null) { _db.Videos.Remove(video); await _db.SaveChangesAsync(); }
    }

    public async Task<bool> YouTubeVideoIdExistsAsync(string videoId, int? excludeId = null) =>
        await _db.Videos.AnyAsync(v => v.YouTubeVideoId == videoId && (!excludeId.HasValue || v.Id != excludeId.Value));

    public async Task<IEnumerable<VideoCategory>> GetCategoriesAsync() =>
        await _db.VideoCategories.OrderBy(c => c.Name).ToListAsync();

    public async Task<VideoCategory?> GetCategoryByIdAsync(int id) =>
        await _db.VideoCategories.FindAsync(id);

    public async Task<VideoCategory> CreateCategoryAsync(VideoCategory category)
    {
        _db.VideoCategories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<VideoCategory> UpdateCategoryAsync(VideoCategory category)
    {
        _db.VideoCategories.Update(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        var category = await _db.VideoCategories.FindAsync(id);
        if (category == null) return false;
        _db.VideoCategories.Remove(category);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CategoryInUseAsync(int id) =>
        await _db.Videos.AnyAsync(v => v.CategoryId == id);
}
