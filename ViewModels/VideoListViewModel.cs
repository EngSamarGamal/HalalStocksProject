using HalalStocks.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HalalStocks.ViewModels;

public class VideoListViewModel
{
    public IEnumerable<Video> Videos { get; set; } = new List<Video>();
    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
    public string? Search { get; set; }
    public int? CategoryId { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
}
