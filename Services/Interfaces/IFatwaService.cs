using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

public interface IFatwaService
{
    // ── Public ──
    Task<(IEnumerable<FatwaQuestion> Items, int Total)> GetPublishedAsync(string? search, int page, int pageSize);
    Task<IEnumerable<FatwaQuestion>> GetFeaturedAsync(int count);

    // ── Asking ──
    Task<int> CountPendingForUserAsync(string userId);
    Task<FatwaQuestion> AskAsync(string userId, string question);

    // ── Admin ──
    Task<(IEnumerable<FatwaQuestion> Items, int Total)> GetForAdminAsync(FatwaStatus? status, int page, int pageSize);
    Task<(int All, int Pending, int Answered, int Published)> GetCountsAsync();
    Task<FatwaQuestion?> GetByIdAsync(int id);

    // Applies the answer + flags and keeps Status/IsPublished/IsFeatured/timestamps consistent.
    Task SaveAnswerAsync(int id, string? answerHtml, bool publish, bool feature);
    Task DeleteAsync(int id);
}
