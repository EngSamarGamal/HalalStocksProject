using HalalStocks.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalalStocks.Controllers.Admin;

[Authorize(Roles = "SuperAdmin,Admin")]
[Route("admin/settings")]
public class SiteSettingsController : Controller
{
    private readonly ISiteSettingsService _settings;

    public SiteSettingsController(ISiteSettingsService settings) => _settings = settings;

    [Route("")]
    public async Task<IActionResult> Index()
    {
        var settings = await _settings.GetSettingsAsync();
        return View("~/Views/Admin/SiteSettings/Index.cshtml", settings);
    }

    [HttpPost, Route("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(HalalStocks.Models.SiteSettings vm)
    {
        await _settings.SaveSettingsAsync(vm);
        TempData["Success"] = "تم حفظ الإعدادات بنجاح";
        return RedirectToAction(nameof(Index));
    }
}
