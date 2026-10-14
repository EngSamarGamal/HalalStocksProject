using HalalStocks.Models;

namespace HalalStocks.ViewModels;

public class StockListViewModel
{
    public IEnumerable<Stock> Stocks { get; set; } = new List<Stock>();
    public IEnumerable<StockCategory> Categories { get; set; } = new List<StockCategory>();
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public ShariahStatus? StatusFilter { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
}
