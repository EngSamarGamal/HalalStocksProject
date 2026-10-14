using HalalStocks.Helpers;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace HalalStocks.Controllers.Admin;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
[Route("admin/articles")]
public class ArticlesController : Controller
{
    private readonly IArticleService _articleService;
    private readonly IStorageService _storage;

    public ArticlesController(IArticleService articleService, IStorageService storage)
    {
        _articleService = articleService;
        _storage        = storage;
    }

    private async Task PopulateFormListsAsync(ArticleFormViewModel vm)
    {
        // Categories whose AutoAssignedForType is set (e.g. "تحليلات وآراء") are hidden from
        // the manual picker — that field is force-set server-side for those article types.
        var categories = await _articleService.GetCategoriesAsync();
        vm.Categories = categories.Where(c => c.AutoAssignedForType == null)
            .Select(c => new SelectListItem(c.NameAr, c.Id.ToString(), c.Id == vm.ArticleCategoryId));

        vm.ArticleTypes = ArticleTypeHelper.All
            .Select(t => new SelectListItem(t.Label, ((int)t.Type).ToString(), t.Type == vm.Type));
    }

    [Route("")]
    public async Task<IActionResult> Index(int page = 1)
    {
        var (articles, total) = await _articleService.GetAllAsync(page, 20);
        ViewBag.TotalPages = (int)Math.Ceiling(total / 20.0);
        ViewBag.CurrentPage = page;
        return View("~/Views/Admin/Articles/Index.cshtml", articles);
    }

    // Resolves the ArticleCategoryId to persist: Analytical articles always use their
    // auto-assigned category (the picker is hidden for that type), everything else
    // requires the admin's explicit choice — enforced here since ArticleCategoryId
    // can't carry a plain [Required] once it's conditionally shown/hidden by type.
    private async Task<int?> ResolveCategoryIdAsync(ArticleFormViewModel vm)
    {
        if (vm.Type == ArticleType.Analytical)
        {
            var autoCategory = await _articleService.GetCategoryForTypeAsync(ArticleType.Analytical);
            return autoCategory?.Id;
        }

        if (!vm.ArticleCategoryId.HasValue)
        {
            ModelState.AddModelError(nameof(vm.ArticleCategoryId), "الرجاء اختيار التصنيف");
            return null;
        }
        return vm.ArticleCategoryId.Value;
    }

    [Route("create")]
    public async Task<IActionResult> Create()
    {
        var vm = new ArticleFormViewModel();
        await PopulateFormListsAsync(vm);
        return View("~/Views/Admin/Articles/Create.cshtml", vm);
    }

    [HttpPost, Route("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ArticleFormViewModel vm)
    {
        var categoryId = ModelState.IsValid ? await ResolveCategoryIdAsync(vm) : null;
        if (!ModelState.IsValid)
        {
            await PopulateFormListsAsync(vm);
            return View("~/Views/Admin/Articles/Create.cshtml", vm);
        }

        var article = new Article
        {
            TitleAr = vm.TitleAr,
            ContentAr = vm.ContentAr,
            ArticleCategoryId = categoryId!.Value,
            Type = vm.Type,
            ApprovalStatus = ArticleApprovalStatus.NotRequired, // admin-authored — no approval gate
            IsPublished = vm.IsPublished,
            PublishedAt = vm.IsPublished ? DateTime.UtcNow : null,
            AuthorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!,
            FeaturedImagePath = await _storage.UploadAsync(vm.FeaturedImage!, "articles")
        };
        await _articleService.CreateAsync(article);
        TempData["Success"] = "تم إضافة المقال بنجاح";
        return Redirect("/admin/articles");
    }

    [Route("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var article = await _articleService.GetByIdAsync(id);
        if (article == null) return NotFound();
        var vm = new ArticleFormViewModel
        {
            Id = article.Id,
            TitleAr = article.TitleAr,
            ContentAr = article.ContentAr,
            ArticleCategoryId = article.ArticleCategoryId,
            ExistingImagePath = article.FeaturedImagePath,
            IsPublished = article.IsPublished,
            Type = article.Type
        };
        await PopulateFormListsAsync(vm);
        return View("~/Views/Admin/Articles/Edit.cshtml", vm);
    }

    [HttpPost, Route("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ArticleFormViewModel vm)
    {
        var categoryId = ModelState.IsValid ? await ResolveCategoryIdAsync(vm) : null;
        if (!ModelState.IsValid)
        {
            await PopulateFormListsAsync(vm);
            return View("~/Views/Admin/Articles/Edit.cshtml", vm);
        }

        var article = await _articleService.GetByIdAsync(id);
        if (article == null) return NotFound();
        article.TitleAr = vm.TitleAr;
        article.ContentAr = vm.ContentAr;
        article.ArticleCategoryId = categoryId!.Value;
        article.Type = vm.Type;
        // ApprovalStatus is intentionally left untouched here — this form represents
        // admin authorship/editing, not the analyst approve/hide workflow (Phase 3).
        article.IsPublished = vm.IsPublished;
        if (vm.IsPublished && !article.PublishedAt.HasValue)
            article.PublishedAt = DateTime.UtcNow;
        else if (!vm.IsPublished)
            article.PublishedAt = null;
        if (vm.FeaturedImage != null)
            article.FeaturedImagePath = await _storage.UploadAsync(vm.FeaturedImage, "articles");
        await _articleService.UpdateAsync(article);
        TempData["Success"] = "تم تحديث المقال بنجاح";
        return Redirect("/admin/articles");
    }

    [HttpPost, Route("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _articleService.DeleteAsync(id);
        TempData["Success"] = "تم حذف المقال";
        return Redirect("/admin/articles");
    }

    [HttpPost, Route("toggle-publish/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePublish(int id)
    {
        await _articleService.TogglePublishAsync(id);
        return Redirect("/admin/articles");
    }

}
