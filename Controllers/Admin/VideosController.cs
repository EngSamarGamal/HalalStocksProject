using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HalalStocks.Controllers.Admin;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
[Route("admin/videos")]
public class VideosController : Controller
{
    private readonly IVideoService _videoService;
    private readonly IYouTubeService _youTubeService;

    public VideosController(IVideoService videoService, IYouTubeService youTubeService)
    {
        _videoService = videoService;
        _youTubeService = youTubeService;
    }

    [Route("")]
    public async Task<IActionResult> Index(int page = 1)
    {
        var (videos, total) = await _videoService.GetAllAsync(page, 20);
        ViewBag.TotalPages = (int)Math.Ceiling(total / 20.0);
        ViewBag.CurrentPage = page;
        return View("~/Views/Admin/Videos/Index.cshtml", videos);
    }

    [Route("create")]
    public async Task<IActionResult> Create()
    {
        var vm = new VideoFormViewModel();
        await PopulateCategoriesAsync(vm);
        return View("~/Views/Admin/Videos/Create.cshtml", vm);
    }

    [HttpPost, Route("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VideoFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(vm);
            return View("~/Views/Admin/Videos/Create.cshtml", vm);
        }

        if (!_youTubeService.TryExtractVideoId(vm.YouTubeUrl, out var videoId))
        {
            ModelState.AddModelError(nameof(vm.YouTubeUrl), "الرجاء إدخال رابط يوتيوب صحيح");
            await PopulateCategoriesAsync(vm);
            return View("~/Views/Admin/Videos/Create.cshtml", vm);
        }

        if (await _videoService.YouTubeVideoIdExistsAsync(videoId))
        {
            ModelState.AddModelError(nameof(vm.YouTubeUrl), "هذا الفيديو مضاف بالفعل");
            await PopulateCategoriesAsync(vm);
            return View("~/Views/Admin/Videos/Create.cshtml", vm);
        }

        var oembed = await _youTubeService.FetchOEmbedAsync(videoId);
        if (!oembed.Ok)
        {
            ModelState.AddModelError(nameof(vm.YouTubeUrl), "لم نتمكن من العثور على هذا الفيديو أو أنه غير متاح للتضمين");
            await PopulateCategoriesAsync(vm);
            return View("~/Views/Admin/Videos/Create.cshtml", vm);
        }

        var title = !string.IsNullOrWhiteSpace(vm.Title) ? vm.Title : oembed.Title;
        if (string.IsNullOrWhiteSpace(title))
        {
            ModelState.AddModelError(nameof(vm.Title), "الرجاء إدخال عنوان الفيديو");
            await PopulateCategoriesAsync(vm);
            return View("~/Views/Admin/Videos/Create.cshtml", vm);
        }

        var video = new Video
        {
            Title = title,
            Description = vm.Description,
            YouTubeUrl = $"https://www.youtube.com/watch?v={videoId}",
            YouTubeVideoId = videoId,
            ThumbnailUrl = oembed.ThumbnailUrl ?? $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg",
            CategoryId = vm.CategoryId
        };
        await _videoService.CreateAsync(video);
        TempData["Success"] = "تم إضافة الفيديو بنجاح";
        return Redirect("/admin/videos");
    }

    [Route("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var video = await _videoService.GetByIdAsync(id);
        if (video == null) return NotFound();

        var vm = new VideoFormViewModel
        {
            Id = video.Id,
            Title = video.Title,
            Description = video.Description,
            YouTubeUrl = video.YouTubeUrl,
            CategoryId = video.CategoryId,
            ThumbnailUrl = video.ThumbnailUrl
        };
        await PopulateCategoriesAsync(vm);
        return View("~/Views/Admin/Videos/Edit.cshtml", vm);
    }

    [HttpPost, Route("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VideoFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCategoriesAsync(vm);
            return View("~/Views/Admin/Videos/Edit.cshtml", vm);
        }

        var video = await _videoService.GetByIdAsync(id);
        if (video == null) return NotFound();

        if (!_youTubeService.TryExtractVideoId(vm.YouTubeUrl, out var videoId))
        {
            ModelState.AddModelError(nameof(vm.YouTubeUrl), "الرجاء إدخال رابط يوتيوب صحيح");
            await PopulateCategoriesAsync(vm);
            return View("~/Views/Admin/Videos/Edit.cshtml", vm);
        }

        if (await _videoService.YouTubeVideoIdExistsAsync(videoId, id))
        {
            ModelState.AddModelError(nameof(vm.YouTubeUrl), "هذا الفيديو مضاف بالفعل");
            await PopulateCategoriesAsync(vm);
            return View("~/Views/Admin/Videos/Edit.cshtml", vm);
        }

        var videoIdChanged = videoId != video.YouTubeVideoId;
        string? oembedTitle = null;
        if (videoIdChanged)
        {
            var oembed = await _youTubeService.FetchOEmbedAsync(videoId);
            if (!oembed.Ok)
            {
                ModelState.AddModelError(nameof(vm.YouTubeUrl), "لم نتمكن من العثور على هذا الفيديو أو أنه غير متاح للتضمين");
                await PopulateCategoriesAsync(vm);
                return View("~/Views/Admin/Videos/Edit.cshtml", vm);
            }
            oembedTitle = oembed.Title;
            video.ThumbnailUrl = oembed.ThumbnailUrl ?? $"https://img.youtube.com/vi/{videoId}/hqdefault.jpg";
        }

        var title = !string.IsNullOrWhiteSpace(vm.Title) ? vm.Title : oembedTitle;
        if (string.IsNullOrWhiteSpace(title))
        {
            ModelState.AddModelError(nameof(vm.Title), "الرجاء إدخال عنوان الفيديو");
            await PopulateCategoriesAsync(vm);
            return View("~/Views/Admin/Videos/Edit.cshtml", vm);
        }

        video.Title = title;
        video.Description = vm.Description;
        video.YouTubeUrl = $"https://www.youtube.com/watch?v={videoId}";
        video.YouTubeVideoId = videoId;
        video.CategoryId = vm.CategoryId;
        await _videoService.UpdateAsync(video);
        TempData["Success"] = "تم تحديث الفيديو بنجاح";
        return Redirect("/admin/videos");
    }

    [HttpPost, Route("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _videoService.DeleteAsync(id);
        TempData["Success"] = "تم حذف الفيديو";
        return Redirect("/admin/videos");
    }

    private async Task PopulateCategoriesAsync(VideoFormViewModel vm)
    {
        var categories = await _videoService.GetCategoriesAsync();
        vm.Categories = categories.Select(c => new SelectListItem(c.Name, c.Id.ToString()));
    }
}
