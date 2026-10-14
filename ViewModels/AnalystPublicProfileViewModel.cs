namespace HalalStocks.ViewModels;

public class AnalystPublicProfileViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? PhotoPath { get; set; }
    public string? SummaryAr { get; set; }

    public IEnumerable<AnalyticalArticleCardViewModel> Articles { get; set; } = new List<AnalyticalArticleCardViewModel>();
    public string? Search { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
}
