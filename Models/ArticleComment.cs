namespace HalalStocks.Models;

/// <summary>
/// Generic on Article so any future article type can carry comments without
/// a schema change. A user can post many comments on the same article.
/// </summary>
public class ArticleComment
{
    public int Id { get; set; }
    public int ArticleId { get; set; }
    public Article Article { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
    public string ContentAr { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
