using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalalStocks.Controllers.Admin;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
[Route("admin/banners")]
public class BannersController : Controller
{
    private readonly IBannerService _bannerService;
    private readonly IStorageService _storage;

    public BannersController(IBannerService bannerService, IStorageService storage)
    {
        _bannerService = bannerService;
        _storage = storage;
    }

    [Route("")]
    public async Task<IActionResult> Index()
    {
        var banners = await _bannerService.GetAllAsync();
        return View("~/Views/Admin/Banners/Index.cshtml", banners);
    }

    [Route("create")]
    public IActionResult Create() => View("~/Views/Admin/Banners/Create.cshtml", new BannerFormViewModel());

    [HttpPost, Route("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BannerFormViewModel vm)
    {
        if (vm.Image == null)
            ModelState.AddModelError(nameof(vm.Image), "الرجاء اختيار صورة للبانر");

        if (!ModelState.IsValid)
            return View("~/Views/Admin/Banners/Create.cshtml", vm);

        var imagePath = await _storage.UploadAsync(vm.Image!, "banners");
        if (imagePath == null)
        {
            ModelState.AddModelError(nameof(vm.Image), "تعذر رفع الصورة، تأكد من الصيغة (JPG, PNG, WebP) والحجم (حتى 2 ميجابايت)");
            return View("~/Views/Admin/Banners/Create.cshtml", vm);
        }

        var banner = new Banner
        {
            TitleAr = vm.TitleAr,
            DescriptionAr = vm.DescriptionAr,
            ButtonText = vm.ButtonText,
            ButtonUrl = vm.ButtonUrl,
            ImagePath = imagePath,
            DisplayOrder = vm.DisplayOrder,
            IsActive = vm.IsActive
        };
        await _bannerService.CreateAsync(banner);
        TempData["Success"] = "تم إضافة البانر بنجاح";
        return Redirect("/admin/banners");
    }

    [Route("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var banner = await _bannerService.GetByIdAsync(id);
        if (banner == null) return NotFound();
        var vm = new BannerFormViewModel
        {
            Id = banner.Id,
            TitleAr = banner.TitleAr,
            DescriptionAr = banner.DescriptionAr,
            ButtonText = banner.ButtonText,
            ButtonUrl = banner.ButtonUrl,
            ExistingImagePath = banner.ImagePath,
            DisplayOrder = banner.DisplayOrder,
            IsActive = banner.IsActive
        };
        return View("~/Views/Admin/Banners/Edit.cshtml", vm);
    }

    [HttpPost, Route("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BannerFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/Banners/Edit.cshtml", vm);

        var banner = await _bannerService.GetByIdAsync(id);
        if (banner == null) return NotFound();

        if (vm.Image != null)
        {
            var imagePath = await _storage.UploadAsync(vm.Image, "banners");
            if (imagePath == null)
            {
                ModelState.AddModelError(nameof(vm.Image), "تعذر رفع الصورة، تأكد من الصيغة (JPG, PNG, WebP) والحجم (حتى 2 ميجابايت)");
                vm.ExistingImagePath = banner.ImagePath;
                return View("~/Views/Admin/Banners/Edit.cshtml", vm);
            }
            await _storage.DeleteAsync(banner.ImagePath);
            banner.ImagePath = imagePath;
        }

        banner.TitleAr = vm.TitleAr;
        banner.DescriptionAr = vm.DescriptionAr;
        banner.ButtonText = vm.ButtonText;
        banner.ButtonUrl = vm.ButtonUrl;
        banner.DisplayOrder = vm.DisplayOrder;
        banner.IsActive = vm.IsActive;
        await _bannerService.UpdateAsync(banner);
        TempData["Success"] = "تم تحديث البانر بنجاح";
        return Redirect("/admin/banners");
    }

    [HttpPost, Route("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var banner = await _bannerService.GetByIdAsync(id);
        if (banner != null)
        {
            await _storage.DeleteAsync(banner.ImagePath);
            await _bannerService.DeleteAsync(id);
            TempData["Success"] = "تم حذف البانر";
        }
        return Redirect("/admin/banners");
    }

    [HttpPost, Route("toggle-active/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        await _bannerService.ToggleActiveAsync(id);
        return Redirect("/admin/banners");
    }
}
