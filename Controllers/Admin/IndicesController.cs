using HalalStocks.Data;
using HalalStocks.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Controllers.Admin;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
[Route("admin/indices")]
public class IndicesController : Controller
{
    private readonly AppDbContext _db;
    public IndicesController(AppDbContext db) => _db = db;

    [Route("")]
    public async Task<IActionResult> Index()
    {
        var indices = await _db.StockIndices
            .Include(i => i.Stocks)
            .OrderBy(i => i.NameAr)
            .ToListAsync();
        return View("~/Views/Admin/Indices/Index.cshtml", indices);
    }

    [Route("create")]
    public IActionResult Create() => View("~/Views/Admin/Indices/Create.cshtml", new StockIndex());

    [HttpPost, Route("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StockIndex model)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/Indices/Create.cshtml", model);

        _db.StockIndices.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "تم إضافة المؤشر بنجاح";
        return Redirect("/admin/indices");
    }

    [Route("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var index = await _db.StockIndices.FindAsync(id);
        if (index == null) return NotFound();
        return View("~/Views/Admin/Indices/Edit.cshtml", index);
    }

    [HttpPost, Route("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StockIndex model)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/Indices/Edit.cshtml", model);

        var index = await _db.StockIndices.FindAsync(id);
        if (index == null) return NotFound();
        index.NameAr = model.NameAr;
        index.NameEn = model.NameEn;
        index.CommonName = model.CommonName;
        index.Description = model.Description;
        await _db.SaveChangesAsync();
        TempData["Success"] = "تم تحديث المؤشر بنجاح";
        return Redirect("/admin/indices");
    }

    [HttpPost, Route("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var index = await _db.StockIndices.FindAsync(id);
        if (index != null) { _db.StockIndices.Remove(index); await _db.SaveChangesAsync(); }
        TempData["Success"] = "تم حذف المؤشر";
        return Redirect("/admin/indices");
    }
}
