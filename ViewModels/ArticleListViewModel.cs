using HalalStocks.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HalalStocks.ViewModels;

public class ArticleListViewModel
{
    public IEnumerable<Article> Articles { get; set; } = new List<Article>();
    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
}
