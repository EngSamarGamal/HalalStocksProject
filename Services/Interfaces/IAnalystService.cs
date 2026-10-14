using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

/// <summary>
/// Owns the analyst self-service area: the public AnalystProfile (bio + photo) and
/// CRUD over the analyst's own Analytical articles, including the approval state
/// machine (Pending -> Approved/Hidden by an admin, back to Pending on re-edit).
/// Every article operation is ownership-checked against the calling userId — a
/// controller never needs to (and must not) trust a posted AuthorId.
/// </summary>
public interface IAnalystService
{
    Task<AnalystProfile?> GetProfileAsync(string userId);
    Task UpdateProfileAsync(string userId, string? summaryAr);

    // Uploads the new photo, saves it, then deletes the old file (in that order, so a
    // failed upload never orphans-then-loses the existing photo). Returns false with an
    // error message if the file fails IStorageService's own validation (type/size).
    Task<(bool Success, string? Error)> UpdatePhotoAsync(string userId, IFormFile photo);
    Task RemovePhotoAsync(string userId);

    Task<(IEnumerable<Article> Items, int TotalCount)> GetOwnArticlesAsync(string userId, int page, int pageSize);

    // Admin "التحليلات والآراء" listing (Controllers/Admin/AnalyticsController) — every
    // AnalystProfile with its owning AppUser loaded, and a (Total, Pending) article count
    // per analyst so the admin list can show at a glance who has work waiting on them.
    Task<IEnumerable<AnalystProfile>> GetAllProfilesAsync();
    Task<(int Total, int Pending)> GetArticleCountsAsync(string userId);

    // Bulk profile lookup for card rendering — a list of analytical articles needs each
    // author's photo, and fetching them one-by-one per card would be N+1. Authors with no
    // AnalystProfile (e.g. an Admin who authored an analytical article) are simply absent
    // from the dictionary; callers fall back to the AppUser's name + default avatar.
    Task<IDictionary<string, AnalystProfile>> GetProfilesByUserIdsAsync(IEnumerable<string> userIds);

    // Returns null if the article doesn't exist, isn't Analytical, or isn't owned by
    // userId — callers must treat null as "not found", not distinguish the reasons.
    Task<Article?> GetOwnArticleByIdAsync(string userId, int articleId);

    Task<Article> CreateArticleAsync(string userId, string titleAr, string contentAr, string? featuredImagePath);

    Task<(bool Success, string? Error)> UpdateArticleAsync(
        string userId, int articleId, string titleAr, string contentAr, string? featuredImagePath);

    Task<(bool Success, string? Error)> DeleteArticleAsync(string userId, int articleId);
}
