using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HalalStocks.Controllers;

public class VideosController : Controller
{
    private readonly IVideoService _videoService;
    private const int PageSize = 12;
    public VideosController(IVideoService videoService) => _videoService = videoService;

    public async Task<IActionResult> Index(string? search, int? category, int page = 1)
    {
        var (videos, total) = await _videoService.GetPublicAsync(search, category, page, PageSize);
        var categories = await _videoService.GetCategoriesAsync();

        return View(new VideoListViewModel
        {
            Videos = videos,
            Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == category)),
            Search = search,
            CategoryId = category,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(total / (double)PageSize)
        });
    }
}
