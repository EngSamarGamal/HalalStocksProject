using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalalStocks.Controllers.Admin;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
[Route("admin/article-categories")]
public class ArticleCategoriesController : Controller
{
    private readonly IArticleService _articleService;

    public ArticleCategoriesController(IArticleService articleService)
    {
        _articleService = articleService;
    }

    [Route("")]
    public async Task<IActionResult> Index()
    {
        var categories = await _articleService.GetCategoriesAsync();
        return View("~/Views/Admin/ArticleCategories/Index.cshtml", categories);
    }

    [Route("create")]
    public IActionResult Create() =>
        View("~/Views/Admin/ArticleCategories/Create.cshtml", new ArticleCategoryFormViewModel());

    [HttpPost, Route("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ArticleCategoryFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/ArticleCategories/Create.cshtml", vm);

        await _articleService.CreateCategoryAsync(new ArticleCategory { NameAr = vm.NameAr });
        TempData["Success"] = "تم إضافة التصنيف بنجاح";
        return Redirect("/admin/article-categories");
    }

    [Route("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await _articleService.GetCategoryByIdAsync(id);
        if (category == null) return NotFound();
        return View("~/Views/Admin/ArticleCategories/Edit.cshtml",
            new ArticleCategoryFormViewModel { Id = category.Id, NameAr = category.NameAr });
    }

    [HttpPost, Route("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ArticleCategoryFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/ArticleCategories/Edit.cshtml", vm);

        var category = await _articleService.GetCategoryByIdAsync(id);
        if (category == null) return NotFound();
        category.NameAr = vm.NameAr;
        await _articleService.UpdateCategoryAsync(category);
        TempData["Success"] = "تم تحديث التصنيف";
        return Redirect("/admin/article-categories");
    }

    [HttpPost, Route("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (id == IArticleService.DefaultCategoryId)
        {
            TempData["DeleteError"] = "لا يمكن حذف التصنيف الافتراضي «عام»";
            return Redirect("/admin/article-categories");
        }

        var category = await _articleService.GetCategoryByIdAsync(id);
        if (category?.AutoAssignedForType != null)
        {
            TempData["DeleteError"] = "لا يمكن حذف هذا التصنيف لأنه مخصص تلقائياً لنوع مقالات معين";
            return Redirect("/admin/article-categories");
        }

        if (await _articleService.CategoryInUseAsync(id))
        {
            TempData["DeleteError"] = "لا يمكن حذف هذا التصنيف لوجود مقالات مرتبطة به";
            return Redirect("/admin/article-categories");
        }

        await _articleService.DeleteCategoryAsync(id);
        TempData["Success"] = "تم حذف التصنيف";
        return Redirect("/admin/article-categories");
    }

    // Inline "add category" affordance used from the article Create/Edit form.
    [HttpPost, Route("quick-create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickCreate([FromBody] ArticleCategoryFormViewModel vm)
    {
        var name = vm?.NameAr?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { message = "الرجاء إدخال اسم التصنيف" });

        var existing = await _articleService.GetCategoriesAsync();
        if (existing.Any(c => c.NameAr == name))
            return BadRequest(new { message = "يوجد تصنيف بهذا الاسم بالفعل" });

        var category = await _articleService.CreateCategoryAsync(new ArticleCategory { NameAr = name });
        return Json(new { id = category.Id, nameAr = category.NameAr });
    }
}
