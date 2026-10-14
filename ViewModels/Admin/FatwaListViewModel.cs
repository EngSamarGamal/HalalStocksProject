using HalalStocks.Models;

namespace HalalStocks.ViewModels.Admin;

public class FatwaListViewModel
{
    public IEnumerable<FatwaQuestion> Questions { get; set; } = new List<FatwaQuestion>();
    public FatwaStatus? Status { get; set; }
    public int AllCount { get; set; }
    public int PendingCount { get; set; }
    public int AnsweredCount { get; set; }
    public int PublishedCount { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
}
