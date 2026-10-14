using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Services.Implementations;

public class ArticleEngagementService : IArticleEngagementService
{
    private readonly AppDbContext _db;
    public ArticleEngagementService(AppDbContext db) => _db = db;

    public async Task<int> GetLikeCountAsync(int articleId) =>
        await _db.ArticleLikes.CountAsync(l => l.ArticleId == articleId);

    public async Task<bool> HasLikedAsync(int articleId, string userId) =>
        await _db.ArticleLikes.AnyAsync(l => l.ArticleId == articleId && l.UserId == userId);

    public async Task<(bool IsLiked, int LikeCount)> ToggleLikeAsync(int articleId, string userId)
    {
        var existing = await _db.ArticleLikes
            .FirstOrDefaultAsync(l => l.ArticleId == articleId && l.UserId == userId);

        if (existing != null)
        {
            _db.ArticleLikes.Remove(existing);
        }
        else
        {
            _db.ArticleLikes.Add(new ArticleLike { ArticleId = articleId, UserId = userId });
        }

        await _db.SaveChangesAsync();

        var count = await GetLikeCountAsync(articleId);
        return (existing == null, count);
    }

    public async Task<IEnumerable<ArticleComment>> GetCommentsAsync(int articleId) =>
        await _db.ArticleComments.Include(c => c.User)
            .Where(c => c.ArticleId == articleId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

    public async Task<ArticleComment> AddCommentAsync(int articleId, string userId, string contentAr)
    {
        var comment = new ArticleComment
        {
            ArticleId = articleId,
            UserId = userId,
            ContentAr = contentAr,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ArticleComments.Add(comment);
        await _db.SaveChangesAsync();

        // Re-load with User so the caller can render the author without a second round trip.
        await _db.Entry(comment).Reference(c => c.User).LoadAsync();
        return comment;
    }

    public async Task<bool> UpdateCommentAsync(int commentId, string userId, string contentAr)
    {
        var comment = await _db.ArticleComments.FirstOrDefaultAsync(c => c.Id == commentId);
        if (comment == null || comment.UserId != userId) return false;

        comment.ContentAr = contentAr;
        comment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCommentAsync(int commentId, string userId)
    {
        var comment = await _db.ArticleComments.FirstOrDefaultAsync(c => c.Id == commentId);
        if (comment == null || comment.UserId != userId) return false;

        _db.ArticleComments.Remove(comment);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCommentAsAdminAsync(int commentId)
    {
        var comment = await _db.ArticleComments.FirstOrDefaultAsync(c => c.Id == commentId);
        if (comment == null) return false;

        _db.ArticleComments.Remove(comment);
        await _db.SaveChangesAsync();
        return true;
    }
}
