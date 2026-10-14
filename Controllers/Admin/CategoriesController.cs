using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalalStocks.Controllers.Admin;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
[Route("admin/categories")]
public class CategoriesController : Controller
{
    private readonly IStockService _stockService;
    private readonly AppDbContext _db;

    public CategoriesController(IStockService stockService, AppDbContext db)
    {
        _stockService = stockService;
        _db = db;
    }

    [Route("")]
    public async Task<IActionResult> Index()
    {
        var categories = await _stockService.GetCategoriesAsync();
        return View("~/Views/Admin/Categories/Index.cshtml", categories);
    }

    [Route("create")]
    public IActionResult Create() =>
        View("~/Views/Admin/Categories/Create.cshtml", new CategoryFormViewModel());

    [HttpPost, Route("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/Categories/Create.cshtml", vm);

        var cat = new StockCategory { NameAr = vm.NameAr, NameEn = vm.NameEn, Description = vm.Description };
        _db.StockCategories.Add(cat);
        await _db.SaveChangesAsync();
        TempData["Success"] = "تم إضافة الفئة بنجاح";
        return Redirect("/admin/categories");
    }

    [Route("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var cat = await _db.StockCategories.FindAsync(id);
        if (cat == null) return NotFound();
        return View("~/Views/Admin/Categories/Edit.cshtml",
            new CategoryFormViewModel { Id = cat.Id, NameAr = cat.NameAr, NameEn = cat.NameEn, Description = cat.Description });
    }

    [HttpPost, Route("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, CategoryFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/Categories/Edit.cshtml", vm);

        var cat = await _db.StockCategories.FindAsync(id);
        if (cat == null) return NotFound();
        cat.NameAr = vm.NameAr;
        cat.NameEn = vm.NameEn;
        cat.Description = vm.Description;
        await _db.SaveChangesAsync();
        TempData["Success"] = "تم تحديث الفئة";
        return Redirect("/admin/categories");
    }

    [HttpPost, Route("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await _db.StockCategories.FindAsync(id);
        if (cat != null) { _db.StockCategories.Remove(cat); await _db.SaveChangesAsync(); }
        TempData["Success"] = "تم حذف الفئة";
        return Redirect("/admin/categories");
    }
}
