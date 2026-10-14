using HalalStocks.Models;

namespace HalalStocks.ViewModels;

public class HomeViewModel
{
    public IEnumerable<Stock> Stocks { get; set; } = new List<Stock>();
    public IEnumerable<StockCategory> Categories { get; set; } = new List<StockCategory>();
    public IEnumerable<Article> LatestArticles { get; set; } = new List<Article>();
    public IEnumerable<Article> ShariahStandards { get; set; } = new List<Article>();
    public IEnumerable<AnalyticalArticleCardViewModel> LatestAnalytical { get; set; } = new List<AnalyticalArticleCardViewModel>();
    public IEnumerable<Video> LatestVideos { get; set; } = new List<Video>();
    public IEnumerable<Banner> Banners { get; set; } = new List<Banner>();
    public IEnumerable<FatwaQuestion> FeaturedFatawa { get; set; } = new List<FatwaQuestion>();
    public string? HomeAlertsHtml { get; set; }
    public IEnumerable<StockIndex> Indices { get; set; } = new List<StockIndex>();
    public string? Search { get; set; }
    public List<int> CategoryIds { get; set; } = new();
    public List<int> IndexIds { get; set; } = new();
    public ShariahStatus? ShariahStatus { get; set; }
    public bool Pure { get; set; }
    public bool SemiPure { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public bool SearchSubmitted { get; set; }
}
