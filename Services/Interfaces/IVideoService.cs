using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

public interface IVideoService
{
    Task<IEnumerable<Video>> GetLatestAsync(int count);
    Task<(IEnumerable<Video> Items, int TotalCount)> GetPublicAsync(string? search, int? categoryId, int page, int pageSize);
    Task<(IEnumerable<Video> Items, int TotalCount)> GetAllAsync(int page, int pageSize);
    Task<Video?> GetByIdAsync(int id);
    Task<Video> CreateAsync(Video video);
    Task<Video> UpdateAsync(Video video);
    Task DeleteAsync(int id);
    Task<bool> YouTubeVideoIdExistsAsync(string videoId, int? excludeId = null);

    Task<IEnumerable<VideoCategory>> GetCategoriesAsync();
    Task<VideoCategory?> GetCategoryByIdAsync(int id);
    Task<VideoCategory> CreateCategoryAsync(VideoCategory category);
    Task<VideoCategory> UpdateCategoryAsync(VideoCategory category);
    Task<bool> DeleteCategoryAsync(int id);
    Task<bool> CategoryInUseAsync(int id);
}
