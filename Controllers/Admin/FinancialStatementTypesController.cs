using HalalStocks.Data;
using HalalStocks.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Controllers.Admin;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
[Route("admin/financial-statement-types")]
public class FinancialStatementTypesController : Controller
{
    private readonly AppDbContext _db;
    public FinancialStatementTypesController(AppDbContext db) => _db = db;

    [Route("")]
    public async Task<IActionResult> Index()
    {
        var types = await _db.FinancialStatementTypes
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.NameAr)
            .ToListAsync();
        return View("~/Views/Admin/FinancialStatementTypes/Index.cshtml", types);
    }

    [Route("create")]
    public IActionResult Create() =>
        View("~/Views/Admin/FinancialStatementTypes/Create.cshtml", new FinancialStatementType());

    [HttpPost, Route("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FinancialStatementType model)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/FinancialStatementTypes/Create.cshtml", model);

        _db.FinancialStatementTypes.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "تم إضافة نوع القوائم المالية بنجاح";
        return Redirect("/admin/financial-statement-types");
    }

    [Route("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var type = await _db.FinancialStatementTypes.FindAsync(id);
        if (type == null) return NotFound();
        return View("~/Views/Admin/FinancialStatementTypes/Edit.cshtml", type);
    }

    [HttpPost, Route("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FinancialStatementType model)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/FinancialStatementTypes/Edit.cshtml", model);

        var type = await _db.FinancialStatementTypes.FindAsync(id);
        if (type == null) return NotFound();
        type.NameAr = model.NameAr;
        type.NameEn = model.NameEn;
        type.DisplayOrder = model.DisplayOrder;
        await _db.SaveChangesAsync();
        TempData["Success"] = "تم تحديث نوع القوائم المالية بنجاح";
        return Redirect("/admin/financial-statement-types");
    }

    [HttpPost, Route("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var type = await _db.FinancialStatementTypes.FindAsync(id);
        if (type != null) { _db.FinancialStatementTypes.Remove(type); await _db.SaveChangesAsync(); }
        TempData["Success"] = "تم حذف نوع القوائم المالية";
        return Redirect("/admin/financial-statement-types");
    }
}
