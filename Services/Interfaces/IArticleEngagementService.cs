using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

/// <summary>
/// Likes and comments. Deliberately keyed on ArticleId rather than scoped to Analytical
/// articles, so enabling engagement on another ArticleType later is a view change only.
/// Ownership rules: a user may edit/delete only their own comment; an admin may delete any.
/// </summary>
public interface IArticleEngagementService
{
    Task<int> GetLikeCountAsync(int articleId);
    Task<bool> HasLikedAsync(int articleId, string userId);

    // Returns the resulting (IsLiked, LikeCount) so an AJAX caller can update in one round
    // trip. Idempotent per user by the unique (ArticleId, UserId) index.
    Task<(bool IsLiked, int LikeCount)> ToggleLikeAsync(int articleId, string userId);

    Task<IEnumerable<ArticleComment>> GetCommentsAsync(int articleId);
    Task<ArticleComment> AddCommentAsync(int articleId, string userId, string contentAr);

    // Ownership-checked: false when the comment doesn't exist or isn't the user's.
    Task<bool> UpdateCommentAsync(int commentId, string userId, string contentAr);
    Task<bool> DeleteCommentAsync(int commentId, string userId);

    // Admin moderation — no ownership check, so it must only ever be called from a
    // role-gated action.
    Task<bool> DeleteCommentAsAdminAsync(int commentId);
}
