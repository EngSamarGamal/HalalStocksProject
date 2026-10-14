namespace HalalStocks.Models;

/// <summary>
/// One row per (Article, User) like. Generic on Article so any future article
/// type can carry likes without a schema change — enforced by a unique index
/// on (ArticleId, UserId) in AppDbContext.
/// </summary>
public class ArticleLike
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
