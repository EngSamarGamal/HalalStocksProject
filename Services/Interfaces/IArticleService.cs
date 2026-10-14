using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

public interface IArticleService
{
    const int DefaultCategoryId = 1;

    Task<(IEnumerable<Article> Items, int TotalCount)> GetPublishedAsync(string? search, int? categoryId, int page, int pageSize);
    Task<Article?> GetByIdAsync(int id);
    Task<IEnumerable<Article>> GetLatestAsync(int count);
    Task<IEnumerable<Article>> GetShariahStandardsAsync(int count);
    Task<(IEnumerable<Article> Items, int TotalCount)> GetAllAsync(int page, int pageSize);
    Task<Article> CreateAsync(Article article);
    Task<Article> UpdateAsync(Article article);
    Task DeleteAsync(int id);
    Task TogglePublishAsync(int id);

    // Admin "التحليلات والآراء" section (Controllers/Admin/AnalyticsController) — every
    // Analytical article regardless of status, and the Pending-only queue that feeds it.
    Task<(IEnumerable<Article> Items, int TotalCount)> GetAnalyticalArticlesAsync(int page, int pageSize);
    Task<(IEnumerable<Article> Items, int TotalCount)> GetPendingArticlesAsync(int page, int pageSize);

    // Cheap counts for the admin tab-label badges — avoids fetching a full page just to
    // read a count for the two tabs that aren't currently active.
    Task<int> GetAnalyticalArticleCountAsync();
    Task<int> GetPendingArticleCountAsync();

    // Public تحليلات وآراء surfaces. All three enforce IsPublished && Type == Analytical,
    // so an unapproved/hidden analyst article can never leak through any of them.
    // Search spans article content AND the author's name, per the client's requirement.
    Task<IEnumerable<Article>> GetLatestAnalyticalAsync(int count);
    Task<(IEnumerable<Article> Items, int TotalCount)> GetPublicAnalyticalAsync(string? search, int page, int pageSize);
    Task<(IEnumerable<Article> Items, int TotalCount)> GetPublicAnalyticalByAuthorAsync(string authorId, string? search, int page, int pageSize);

    // Admin approval-state transitions — see Models/ArticleType.cs for the invariant
    // (IsPublished == true implies ApprovalStatus is NotRequired or Approved). Analyst-side
    // transitions (create -> Pending, edit-while-live -> back to Pending) live on
    // IAnalystService instead, since those are ownership-checked self-service operations.
    Task ApproveArticleAsync(int id);
    Task HideArticleAsync(int id, string reason);

    Task<IEnumerable<ArticleCategory>> GetCategoriesAsync();
    Task<ArticleCategory?> GetCategoryByIdAsync(int id);
    Task<ArticleCategory?> GetDefaultCategoryAsync();

    // Resolves the auto-assigned category for a given ArticleType (e.g. "تحليلات وآراء"
    // for Analytical), never by name or a fixed Id — used to force-categorize articles
    // whose type manages its own category rather than exposing a picker (see Analytical).
    Task<ArticleCategory?> GetCategoryForTypeAsync(ArticleType type);

    Task<ArticleCategory> CreateCategoryAsync(ArticleCategory category);
    Task<ArticleCategory> UpdateCategoryAsync(ArticleCategory category);
    Task<bool> DeleteCategoryAsync(int id);
    Task<bool> CategoryInUseAsync(int id);
    Task<int> CategoryArticleCountAsync(int id);
}
