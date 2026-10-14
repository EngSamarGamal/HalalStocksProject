namespace HalalStocks.Models;

public class ArticleCategory
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;

    // When set, this category is the auto-assigned home for every article of that
    // ArticleType (e.g. "تحليلات وآراء" for ArticleType.Analytical) — resolved at
    // runtime via IArticleService.GetCategoryForTypeAsync, never by name or a fixed Id,
    // so renaming the category is harmless. A category with this set is undeletable,
    // same protection as the seeded "عام" default.
    public ArticleType? AutoAssignedForType { get; set; }

    public ICollection<Article> Articles { get; set; } = new List<Article>();
}
