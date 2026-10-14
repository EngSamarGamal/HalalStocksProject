using HalalStocks.Models;

namespace HalalStocks.ViewModels;

public class AnalystArticleListViewModel
{
    public IEnumerable<Article> Articles { get; set; } = new List<Article>();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
}
