using HalalStocks.Models;

namespace HalalStocks.ViewModels;

public class ArticleDetailViewModel
{
    public Article Article { get; set; } = null!;

    // Analyst byline — populated only for ArticleType.Analytical.
    public string? AuthorName { get; set; }
    public string? AuthorPhotoPath { get; set; }

    // Engagement is currently surfaced on analytical articles only; the schema and service
    // are generic, so switching this on for another type is a controller change, not a
    // data-model one.
    public bool ShowEngagement { get; set; }
    public int LikeCount { get; set; }
    public bool HasLiked { get; set; }
    public IEnumerable<ArticleComment> Comments { get; set; } = new List<ArticleComment>();

    public bool IsAuthenticated { get; set; }
    public string? CurrentUserId { get; set; }
    public bool CanModerateComments { get; set; }
}
