using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Controllers.Admin;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
[Route("admin/portfolio-sections")]
public class PortfolioSectionsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IStorageService _storage;

    public PortfolioSectionsController(AppDbContext db, IStorageService storage)
    {
        _db      = db;
        _storage = storage;
    }

    [Route("")]
    public async Task<IActionResult> Index()
    {
        var sections = await _db.PortfolioInfoSections
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();
        return View("~/Views/Admin/PortfolioSections/Index.cshtml", sections);
    }

    [Route("create")]
    public IActionResult Create() =>
        View("~/Views/Admin/PortfolioSections/Create.cshtml", new PortfolioSectionFormViewModel());

    [HttpPost, Route("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PortfolioSectionFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/PortfolioSections/Create.cshtml", vm);

        var section = new PortfolioInfoSection
        {
            Title        = vm.Title,
            Content      = vm.Content,
            DisplayOrder = vm.DisplayOrder,
            IsActive     = vm.IsActive
        };

        if (vm.Image != null)
            section.ImagePath = await _storage.UploadAsync(vm.Image, "portfolio", maxBytes: 20 * 1024 * 1024);

        _db.PortfolioInfoSections.Add(section);
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم إضافة القسم بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [Route("edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        var section = await _db.PortfolioInfoSections.FindAsync(id);
        if (section == null) return NotFound();

        var vm = new PortfolioSectionFormViewModel
        {
            Id                = section.Id,
            Title             = section.Title,
            Content           = section.Content,
            ExistingImagePath = section.ImagePath,
            DisplayOrder      = section.DisplayOrder,
            IsActive          = section.IsActive
        };
        return View("~/Views/Admin/PortfolioSections/Edit.cshtml", vm);
    }

    [HttpPost, Route("edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PortfolioSectionFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/PortfolioSections/Edit.cshtml", vm);

        var section = await _db.PortfolioInfoSections.FindAsync(id);
        if (section == null) return NotFound();

        section.Title        = vm.Title;
        section.Content      = vm.Content;
        section.DisplayOrder = vm.DisplayOrder;
        section.IsActive     = vm.IsActive;
        section.UpdatedAt    = DateTime.UtcNow;

        if (vm.Image != null)
        {
            await _storage.DeleteAsync(section.ImagePath);
            section.ImagePath = await _storage.UploadAsync(vm.Image, "portfolio", maxBytes: 20 * 1024 * 1024);
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "تم تحديث القسم بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Route("delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var section = await _db.PortfolioInfoSections.FindAsync(id);
        if (section == null) return NotFound();

        await _storage.DeleteAsync(section.ImagePath);
        _db.PortfolioInfoSections.Remove(section);
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم حذف القسم";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Route("move-up/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveUp(int id)
    {
        var section = await _db.PortfolioInfoSections.FindAsync(id);
        if (section == null) return NotFound();

        var prev = await _db.PortfolioInfoSections
            .Where(s => s.DisplayOrder < section.DisplayOrder)
            .OrderByDescending(s => s.DisplayOrder)
            .FirstOrDefaultAsync();

        if (prev != null)
            (prev.DisplayOrder, section.DisplayOrder) = (section.DisplayOrder, prev.DisplayOrder);

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Route("move-down/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveDown(int id)
    {
        var section = await _db.PortfolioInfoSections.FindAsync(id);
        if (section == null) return NotFound();

        var next = await _db.PortfolioInfoSections
            .Where(s => s.DisplayOrder > section.DisplayOrder)
            .OrderBy(s => s.DisplayOrder)
            .FirstOrDefaultAsync();

        if (next != null)
            (next.DisplayOrder, section.DisplayOrder) = (section.DisplayOrder, next.DisplayOrder);

        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

}
