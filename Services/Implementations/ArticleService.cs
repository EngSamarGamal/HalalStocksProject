using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Services.Implementations;

public class ArticleService : IArticleService
{
    private readonly AppDbContext _db;
    private readonly IStorageService _storage;

    public ArticleService(AppDbContext db, IStorageService storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<(IEnumerable<Article> Items, int TotalCount)> GetPublishedAsync(string? search, int? categoryId, int page, int pageSize)
    {
        var query = _db.Articles.Include(a => a.Author).Include(a => a.ArticleCategory)
            .Where(a => a.IsPublished && a.Type != ArticleType.Analytical);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.TitleAr.Contains(search) || a.ContentAr.Contains(search));

        if (categoryId.HasValue)
            query = query.Where(a => a.ArticleCategoryId == categoryId.Value);

        query = query.OrderByDescending(a => a.PublishedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<Article?> GetByIdAsync(int id) =>
        await _db.Articles.Include(a => a.Author).Include(a => a.ArticleCategory).FirstOrDefaultAsync(a => a.Id == id);

    public async Task<IEnumerable<Article>> GetLatestAsync(int count) =>
        await _db.Articles.Include(a => a.Author).Include(a => a.ArticleCategory)
            .Where(a => a.IsPublished && a.Type == ArticleType.Standard).OrderByDescending(a => a.PublishedAt)
            .Take(count).ToListAsync();

    public async Task<IEnumerable<Article>> GetShariahStandardsAsync(int count) =>
        await _db.Articles.Include(a => a.Author).Include(a => a.ArticleCategory)
            .Where(a => a.IsPublished && a.Type == ArticleType.ShariahStandard).OrderByDescending(a => a.PublishedAt)
            .Take(count).ToListAsync();

    public async Task<(IEnumerable<Article> Items, int TotalCount)> GetAllAsync(int page, int pageSize)
    {
        var query = _db.Articles.Include(a => a.Author).Include(a => a.ArticleCategory).OrderByDescending(a => a.CreatedAt);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<Article> CreateAsync(Article article)
    {
        article.CreatedAt = DateTime.UtcNow;
        article.UpdatedAt = DateTime.UtcNow;
        _db.Articles.Add(article);
        await _db.SaveChangesAsync();
        return article;
    }

    public async Task<Article> UpdateAsync(Article article)
    {
        article.UpdatedAt = DateTime.UtcNow;
        _db.Articles.Update(article);
        await _db.SaveChangesAsync();
        return article;
    }

    public async Task DeleteAsync(int id)
    {
        var article = await _db.Articles.FindAsync(id);
        if (article == null) return;

        var imagePath = article.FeaturedImagePath;
        _db.Articles.Remove(article);
        await _db.SaveChangesAsync();

        // Clean up the featured image so deleting an article doesn't orphan its R2 object.
        // Previously omitted here (a documented gap), which meant admin-side deletes leaked
        // files while the analyst-side delete did not — same action, inconsistent result.
        if (imagePath != null) await _storage.DeleteAsync(imagePath);
    }

    public async Task TogglePublishAsync(int id)
    {
        var article = await _db.Articles.FindAsync(id);
        if (article == null) return;
        article.IsPublished = !article.IsPublished;
        article.PublishedAt = article.IsPublished ? DateTime.UtcNow : null;
        article.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<(IEnumerable<Article> Items, int TotalCount)> GetAnalyticalArticlesAsync(int page, int pageSize)
    {
        var query = _db.Articles.Include(a => a.Author)
            .Where(a => a.Type == ArticleType.Analytical)
            .OrderByDescending(a => a.CreatedAt);
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<(IEnumerable<Article> Items, int TotalCount)> GetPendingArticlesAsync(int page, int pageSize)
    {
        var query = _db.Articles.Include(a => a.Author)
            .Where(a => a.Type == ArticleType.Analytical && a.ApprovalStatus == ArticleApprovalStatus.Pending)
            .OrderBy(a => a.CreatedAt); // oldest-waiting-first, so a queue actually drains
        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    // Shared base for every public Analytical query — the IsPublished + Type gate lives in
    // exactly one place so no public surface can accidentally omit half of it.
    private IQueryable<Article> PublicAnalyticalQuery() =>
        _db.Articles.Include(a => a.Author).Include(a => a.Likes)
            .Where(a => a.IsPublished && a.Type == ArticleType.Analytical);

    public async Task<IEnumerable<Article>> GetLatestAnalyticalAsync(int count) =>
        await PublicAnalyticalQuery()
            .OrderByDescending(a => a.PublishedAt)
            .Take(count).ToListAsync();

    public async Task<(IEnumerable<Article> Items, int TotalCount)> GetPublicAnalyticalAsync(string? search, int page, int pageSize)
    {
        var query = PublicAnalyticalQuery();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a =>
                a.TitleAr.Contains(search) ||
                a.ContentAr.Contains(search) ||
                a.Author.FirstName!.Contains(search) ||
                a.Author.LastName!.Contains(search) ||
                a.Author.FullName.Contains(search));

        query = query.OrderByDescending(a => a.PublishedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<(IEnumerable<Article> Items, int TotalCount)> GetPublicAnalyticalByAuthorAsync(
        string authorId, string? search, int page, int pageSize)
    {
        var query = PublicAnalyticalQuery().Where(a => a.AuthorId == authorId);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(a => a.TitleAr.Contains(search) || a.ContentAr.Contains(search));

        query = query.OrderByDescending(a => a.PublishedAt);

        var total = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items, total);
    }

    public async Task<int> GetAnalyticalArticleCountAsync() =>
        await _db.Articles.CountAsync(a => a.Type == ArticleType.Analytical);

    public async Task<int> GetPendingArticleCountAsync() =>
        await _db.Articles.CountAsync(a => a.Type == ArticleType.Analytical && a.ApprovalStatus == ArticleApprovalStatus.Pending);

    public async Task ApproveArticleAsync(int id)
    {
        var article = await _db.Articles.FindAsync(id);
        if (article == null) return;
        article.ApprovalStatus = ArticleApprovalStatus.Approved;
        article.IsPublished = true;
        if (!article.PublishedAt.HasValue) article.PublishedAt = DateTime.UtcNow;
        article.HideReason = null;
        article.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task HideArticleAsync(int id, string reason)
    {
        var article = await _db.Articles.FindAsync(id);
        if (article == null) return;
        article.ApprovalStatus = ArticleApprovalStatus.Hidden;
        article.IsPublished = false;
        article.PublishedAt = null;
        article.HideReason = reason;
        article.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<ArticleCategory>> GetCategoriesAsync() =>
        await _db.ArticleCategories.Include(c => c.Articles).OrderBy(c => c.NameAr).ToListAsync();

    public async Task<ArticleCategory?> GetCategoryByIdAsync(int id) =>
        await _db.ArticleCategories.FindAsync(id);

    public async Task<ArticleCategory?> GetDefaultCategoryAsync() =>
        await _db.ArticleCategories.FindAsync(IArticleService.DefaultCategoryId);

    public async Task<ArticleCategory?> GetCategoryForTypeAsync(ArticleType type) =>
        await _db.ArticleCategories.FirstOrDefaultAsync(c => c.AutoAssignedForType == type);

    public async Task<ArticleCategory> CreateCategoryAsync(ArticleCategory category)
    {
        _db.ArticleCategories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<ArticleCategory> UpdateCategoryAsync(ArticleCategory category)
    {
        _db.ArticleCategories.Update(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        var category = await _db.ArticleCategories.FindAsync(id);
        if (category == null) return false;
        _db.ArticleCategories.Remove(category);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> CategoryInUseAsync(int id) =>
        await _db.Articles.AnyAsync(a => a.ArticleCategoryId == id);

    public async Task<int> CategoryArticleCountAsync(int id) =>
        await _db.Articles.CountAsync(a => a.ArticleCategoryId == id);
}
