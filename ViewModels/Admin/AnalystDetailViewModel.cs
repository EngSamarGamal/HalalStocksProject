using HalalStocks.Models;

namespace HalalStocks.ViewModels.Admin;

public class AnalystDetailViewModel
{
    public AnalystProfile Profile { get; set; } = null!;
    public IEnumerable<Article> Articles { get; set; } = new List<Article>();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
}
