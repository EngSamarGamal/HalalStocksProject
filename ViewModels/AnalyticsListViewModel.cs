namespace HalalStocks.ViewModels;

public class AnalyticsListViewModel
{
    public IEnumerable<AnalyticalArticleCardViewModel> Articles { get; set; } = new List<AnalyticalArticleCardViewModel>();
    public string? Search { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
}
