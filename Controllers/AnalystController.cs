using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HalalStocks.Controllers;

[Authorize]
[Route("analyst")]
public class AnalystController : Controller
{
    private readonly IAnalystService _analystService;
    private readonly UserManager<AppUser> _userManager;
    private readonly IStorageService _storage;
    private const int PageSize = 10;

    public AnalystController(IAnalystService analystService, UserManager<AppUser> userManager, IStorageService storage)
    {
        _analystService = analystService;
        _userManager = userManager;
        _storage = storage;
    }

    // IsAnalyst is baked into the auth cookie as a claim at sign-in (see
    // AppUserClaimsPrincipalFactory), so a designation made after the user's last login
    // wouldn't be reflected there yet — every action re-checks the DB flag directly rather
    // than trusting [Authorize(Roles = "Analyst")].
    private async Task<AppUser?> GetCurrentAnalystAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user != null && user.IsAnalyst ? user : null;
    }

    [Route("")]
    public IActionResult Index() => Redirect("/analyst/profile");

    [Route("profile")]
    public async Task<IActionResult> Profile()
    {
        var user = await GetCurrentAnalystAsync();
        if (user == null) return Forbid();

        var profile = await _analystService.GetProfileAsync(user.Id);
        return View("~/Views/Analyst/Profile.cshtml", new AnalystProfileFormViewModel
        {
            SummaryAr = profile?.SummaryAr,
            ExistingPhotoPath = profile?.PhotoPath
        });
    }

    [HttpPost, Route("profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(AnalystProfileFormViewModel vm)
    {
        var user = await GetCurrentAnalystAsync();
        if (user == null) return Forbid();

        if (!ModelState.IsValid)
            return View("~/Views/Analyst/Profile.cshtml", vm);

        await _analystService.UpdateProfileAsync(user.Id, vm.SummaryAr);

        if (vm.Photo != null)
        {
            var (success, error) = await _analystService.UpdatePhotoAsync(user.Id, vm.Photo);
            if (!success) TempData["Error"] = error;
        }

        TempData["Success"] = "تم تحديث ملفك التعريفي بنجاح";
        return Redirect("/analyst/profile");
    }

    [HttpPost, Route("profile/photo/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto()
    {
        var user = await GetCurrentAnalystAsync();
        if (user == null) return Forbid();

        await _analystService.RemovePhotoAsync(user.Id);
        TempData["Success"] = "تم حذف الصورة الشخصية";
        return Redirect("/analyst/profile");
    }

    [Route("articles")]
    public async Task<IActionResult> Articles(int page = 1)
    {
        var user = await GetCurrentAnalystAsync();
        if (user == null) return Forbid();

        var (articles, total) = await _analystService.GetOwnArticlesAsync(user.Id, page, PageSize);
        return View("~/Views/Analyst/Articles/Index.cshtml", new AnalystArticleListViewModel
        {
            Articles = articles,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(total / (double)PageSize)
        });
    }

    [Route("articles/create")]
    public async Task<IActionResult> Create()
    {
        var user = await GetCurrentAnalystAsync();
        if (user == null) return Forbid();

        return View("~/Views/Analyst/Articles/Create.cshtml", new AnalystArticleFormViewModel());
    }

    [HttpPost, Route("articles/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AnalystArticleFormViewModel vm)
    {
        var user = await GetCurrentAnalystAsync();
        if (user == null) return Forbid();

        if (!ModelState.IsValid)
            return View("~/Views/Analyst/Articles/Create.cshtml", vm);

        string? imagePath = null;
        if (vm.FeaturedImage != null)
            imagePath = await _storage.UploadAsync(vm.FeaturedImage, "articles");

        await _analystService.CreateArticleAsync(user.Id, vm.TitleAr, vm.ContentAr, imagePath);
        TempData["Success"] = "تم إرسال المقال بانتظار موافقة الإدارة";
        return Redirect("/analyst/articles");
    }

    [Route("articles/edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await GetCurrentAnalystAsync();
        if (user == null) return Forbid();

        var article = await _analystService.GetOwnArticleByIdAsync(user.Id, id);
        if (article == null) return NotFound();

        return View("~/Views/Analyst/Articles/Edit.cshtml", new AnalystArticleFormViewModel
        {
            Id = article.Id,
            TitleAr = article.TitleAr,
            ContentAr = article.ContentAr,
            ExistingImagePath = article.FeaturedImagePath,
            ApprovalStatus = article.ApprovalStatus,
            HideReason = article.HideReason
        });
    }

    [HttpPost, Route("articles/edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AnalystArticleFormViewModel vm)
    {
        var user = await GetCurrentAnalystAsync();
        if (user == null) return Forbid();

        if (!ModelState.IsValid)
            return View("~/Views/Analyst/Articles/Edit.cshtml", vm);

        string? imagePath = null;
        if (vm.FeaturedImage != null)
            imagePath = await _storage.UploadAsync(vm.FeaturedImage, "articles");

        var (success, error) = await _analystService.UpdateArticleAsync(user.Id, id, vm.TitleAr, vm.ContentAr, imagePath);
        if (!success) return NotFound();

        TempData["Success"] = "تم تحديث المقال، وأُعيد إلى قائمة الانتظار لمراجعة الإدارة";
        return Redirect("/analyst/articles");
    }

    [HttpPost, Route("articles/delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await GetCurrentAnalystAsync();
        if (user == null) return Forbid();

        var (success, _) = await _analystService.DeleteArticleAsync(user.Id, id);
        TempData["Success"] = success ? "تم حذف المقال" : "تعذر حذف المقال";
        return Redirect("/analyst/articles");
    }
}
