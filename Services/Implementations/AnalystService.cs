using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Services.Implementations;

public class AnalystService : IAnalystService
{
    private readonly AppDbContext _db;
    private readonly IArticleService _articleService;
    private readonly IStorageService _storage;

    public AnalystService(AppDbContext db, IArticleService articleService, IStorageService storage)
    {
        _db = db;
        _articleService = articleService;
        _storage = storage;
    }

    // Includes User: the analyst's own page only needs SummaryAr/PhotoPath, but the admin
    // detail view renders the owner's display name, and a bare load left it null there.
    public async Task<AnalystProfile?> GetProfileAsync(string userId) =>
        await _db.AnalystProfiles.Include(p => p.User).FirstOrDefaultAsync(p => p.UserId == userId);

    // Defensive: normally UserService.ToggleAnalystAsync already created the profile row
    // when IsAnalyst was first set, but every write here goes through this so a missing
    // row is self-healing rather than a null-reference crash.
    private async Task<AnalystProfile> GetOrCreateProfileAsync(string userId)
    {
        var profile = await GetProfileAsync(userId);
        if (profile != null) return profile;

        profile = new AnalystProfile { UserId = userId };
        _db.AnalystProfiles.Add(profile);
        await _db.SaveChangesAsync();
        return profile;
    }

    public async Task UpdateProfileAsync(string userId, string? summaryAr)
    {
        var profile = await GetOrCreateProfileAsync(userId);
        profile.SummaryAr = summaryAr;
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<(bool Success, string? Error)> UpdatePhotoAsync(string userId, IFormFile photo)
    {
        var profile = await GetOrCreateProfileAsync(userId);
        var newPath = await _storage.UploadAsync(photo, "analysts");
        if (newPath == null) return (false, "الصورة غير صالحة أو حجمها أكبر من 2 ميجابايت");

        var oldPath = profile.PhotoPath;
        profile.PhotoPath = newPath;
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        if (oldPath != null) await _storage.DeleteAsync(oldPath);
        return (true, null);
    }

    public async Task RemovePhotoAsync(string userId)
    {
        var profile = await GetProfileAsync(userId);
        if (profile?.PhotoPath == null) return;

        var oldPath = profile.PhotoPath;
        profile.PhotoPath = null;
        profile.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _storage.DeleteAsync(oldPath);
    }

    // Ordered by CreatedAt, not name — AppUser.FullName is only populated for admin/editor
    // accounts; public-user analysts (the expected case) carry FirstName/LastName instead,
    // same reasoning as UserService.GetPublicUsersAsync.
    public async Task<IEnumerable<AnalystProfile>> GetAllProfilesAsync() =>
        await _db.AnalystProfiles.Include(p => p.User).OrderByDescending(p => p.CreatedAt).ToListAsync();

    public async Task<IDictionary<string, AnalystProfile>> GetProfilesByUserIdsAsync(IEnumerable<string> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<string, AnalystProfile>();

        return await _db.AnalystProfiles
            .Where(p => ids.Contains(p.UserId))
            .ToDictionaryAsync(p => p.UserId);
    }

    public async Task<(int Total, int Pending)> GetArticleCountsAsync(string userId)
    {
        var total = await _db.Articles.CountAsync(a => a.AuthorId == userId && a.Type == ArticleType.Analytical);
        var pending = await _db.Articles.CountAsync(a =>
            a.AuthorId == userId && a.Type == ArticleType.Analytical && a.ApprovalStatus == ArticleApprovalStatus.Pending);
        return (total, pending);
    }

    public async Task<(IEnumerable<Article> Items, int TotalCount)> GetOwnArticlesAsync(string userId, int page, int pageSize)
    {
        var query = _db.Articles
            .Where(a => a.AuthorId == userId && a.Type == ArticleType.Analytical)
            .OrderByDescending(a => a.CreatedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<Article?> GetOwnArticleByIdAsync(string userId, int articleId)
    {
        var article = await _db.Articles.Include(a => a.ArticleCategory)
            .FirstOrDefaultAsync(a => a.Id == articleId);
        return article != null && article.AuthorId == userId && article.Type == ArticleType.Analytical
            ? article
            : null;
    }

    public async Task<Article> CreateArticleAsync(string userId, string titleAr, string contentAr, string? featuredImagePath)
    {
        var category = await _articleService.GetCategoryForTypeAsync(ArticleType.Analytical);

        var article = new Article
        {
            TitleAr = titleAr,
            ContentAr = contentAr,
            ArticleCategoryId = category!.Id,
            Type = ArticleType.Analytical,
            ApprovalStatus = ArticleApprovalStatus.Pending,
            IsPublished = false,
            AuthorId = userId,
            FeaturedImagePath = featuredImagePath,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Articles.Add(article);
        await _db.SaveChangesAsync();
        return article;
    }

    public async Task<(bool Success, string? Error)> UpdateArticleAsync(
        string userId, int articleId, string titleAr, string contentAr, string? featuredImagePath)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == articleId);
        if (article == null) return (false, "المقال غير موجود");
        if (article.AuthorId != userId || article.Type != ArticleType.Analytical)
            return (false, "غير مصرح لك بتعديل هذا المقال");

        article.TitleAr = titleAr;
        article.ContentAr = contentAr;
        if (featuredImagePath != null) article.FeaturedImagePath = featuredImagePath;

        // An edit to a live (Approved) or previously Hidden article pulls it from public
        // view and sends it back to Pending for re-review — the "back to pending" rule.
        // An edit while still Pending is a no-op on the state, just updated content.
        if (article.ApprovalStatus is ArticleApprovalStatus.Approved or ArticleApprovalStatus.Hidden)
        {
            article.ApprovalStatus = ArticleApprovalStatus.Pending;
            article.IsPublished = false;
            article.PublishedAt = null;
            article.HideReason = null;
        }

        article.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteArticleAsync(string userId, int articleId)
    {
        var article = await _db.Articles.FirstOrDefaultAsync(a => a.Id == articleId);
        if (article == null) return (false, "المقال غير موجود");
        if (article.AuthorId != userId || article.Type != ArticleType.Analytical)
            return (false, "غير مصرح لك بحذف هذا المقال");

        // Ownership is verified above; the actual removal (including featured-image cleanup)
        // lives in ArticleService.DeleteAsync so both delete paths behave identically.
        await _articleService.DeleteAsync(articleId);
        return (true, null);
    }
}
