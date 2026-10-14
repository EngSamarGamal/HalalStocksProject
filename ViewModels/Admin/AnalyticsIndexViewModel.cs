using HalalStocks.Models;

namespace HalalStocks.ViewModels.Admin;

public class AnalystSummary
{
    public AnalystProfile Profile { get; set; } = null!;
    public int TotalArticles { get; set; }
    public int PendingArticles { get; set; }
}

public class AnalyticsIndexViewModel
{
    public string Tab { get; set; } = "analysts";
    public int AnalystsCount { get; set; }
    public int ArticlesCount { get; set; }
    public int PendingCount { get; set; }

    // Only the collection matching Tab is populated by the controller.
    public IEnumerable<AnalystSummary> Analysts { get; set; } = new List<AnalystSummary>();
    public IEnumerable<Article> Articles { get; set; } = new List<Article>();

    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
}
