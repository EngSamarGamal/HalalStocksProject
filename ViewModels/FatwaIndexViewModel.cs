using HalalStocks.Models;

namespace HalalStocks.ViewModels;

public class FatwaIndexViewModel
{
    public IEnumerable<FatwaQuestion> Questions { get; set; } = new List<FatwaQuestion>();
    public string? Search { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public bool IsAuthenticated { get; set; }
    public FatwaAskViewModel Ask { get; set; } = new();
}
