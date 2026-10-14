namespace HalalStocks.Models;

public class Article
{
    public int Id { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string ContentAr { get; set; } = string.Empty;
    public string? FeaturedImagePath { get; set; }
    public int ArticleCategoryId { get; set; }
    public ArticleCategory ArticleCategory { get; set; } = null!;
    public bool IsPublished { get; set; }
    public ArticleType Type { get; set; } = ArticleType.Standard;
    public ArticleApprovalStatus ApprovalStatus { get; set; } = ArticleApprovalStatus.NotRequired;
    public string? HideReason { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string AuthorId { get; set; } = string.Empty;
    public AppUser Author { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ArticleLike> Likes { get; set; } = new List<ArticleLike>();
    public ICollection<ArticleComment> Comments { get; set; } = new List<ArticleComment>();
}
