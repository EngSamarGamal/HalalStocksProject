using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalalStocks.Controllers.Admin;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
[Route("admin/video-categories")]
public class VideoCategoriesController : Controller
{
    private readonly IVideoService _videoService;

    public VideoCategoriesController(IVideoService videoService)
    {
        _videoService = videoService;
    }

    [Route("")]
    public async Task<IActionResult> Index()
    {
        var categories = await _videoService.GetCategoriesAsync();
        return View("~/Views/Admin/VideoCategories/Index.cshtml", categories);
    }

    [Route("create")]
    public IActionResult Create() =>
        View("~/Views/Admin/VideoCategories/Create.cshtml", new VideoCategoryFormViewModel());

    [HttpPost, Route("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VideoCategoryFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/VideoCategories/Create.cshtml", vm);

        await _videoService.CreateCategoryAsync(new VideoCategory { Name = vm.Name });
        TempData["Success"] = "تم إضافة الفئة بنجاح";
        return Redirect("/admin/video-categories");
    }

    [Route("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _videoService.GetCategoryByIdAsync(id);
        if (category == null) return NotFound();
        return View("~/Views/Admin/VideoCategories/Edit.cshtml",
            new VideoCategoryFormViewModel { Id = category.Id, Name = category.Name });
    }

    [HttpPost, Route("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VideoCategoryFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/VideoCategories/Edit.cshtml", vm);

        var category = await _videoService.GetCategoryByIdAsync(id);
        if (category == null) return NotFound();
        category.Name = vm.Name;
        await _videoService.UpdateCategoryAsync(category);
        TempData["Success"] = "تم تحديث الفئة";
        return Redirect("/admin/video-categories");
    }

    [HttpPost, Route("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _videoService.CategoryInUseAsync(id))
        {
            TempData["DeleteError"] = "لا يمكن حذف هذه الفئة لوجود فيديوهات مرتبطة بها";
            return Redirect("/admin/video-categories");
        }

        await _videoService.DeleteCategoryAsync(id);
        TempData["Success"] = "تم حذف الفئة";
        return Redirect("/admin/video-categories");
    }
}
