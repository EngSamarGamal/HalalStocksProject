using HalalStocks.Helpers;
using HalalStocks.Models;

namespace HalalStocks.ViewModels;

/// <summary>
/// One analytical article as rendered on a card (homepage strip, /analytics grid,
/// analyst profile page). Built via <see cref="From"/> so every surface shows the
/// same fields with the same fallbacks.
/// </summary>
public class AnalyticalArticleCardViewModel
{
    public int ArticleId { get; set; }
    public string TitleAr { get; set; } = string.Empty;
    public string? FeaturedImagePath { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int LikeCount { get; set; }

    public string AuthorId { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorPhotoPath { get; set; }

    public static IEnumerable<AnalyticalArticleCardViewModel> FromMany(
        IEnumerable<Article> articles, IDictionary<string, AnalystProfile> profilesByUserId) =>
        articles.Select(a => From(a, profilesByUserId.TryGetValue(a.AuthorId, out var p) ? p : null)).ToList();

    public static AnalyticalArticleCardViewModel From(Article article, AnalystProfile? profile) => new()
    {
        ArticleId = article.Id,
        TitleAr = article.TitleAr,
        FeaturedImagePath = article.FeaturedImagePath,
        PublishedAt = article.PublishedAt,
        // Requires the query to have Included Likes — see ArticleService.PublicAnalyticalQuery.
        LikeCount = article.Likes.Count,
        AuthorId = article.AuthorId,
        AuthorName = UserDisplayNameHelper.DisplayName(article.Author),
        // Null when the author has no AnalystProfile (e.g. an admin-authored analytical
        // article); the card falls back to the default person icon.
        AuthorPhotoPath = profile?.PhotoPath
    };
}
