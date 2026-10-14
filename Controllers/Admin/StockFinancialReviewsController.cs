using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Controllers.Admin;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
[Route("admin/stocks/{stockId:int}/reviews")]
public class StockFinancialReviewsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IStorageService _storage;

    private static readonly string[] PdfOnly = { ".pdf" };
    private const long PdfMaxBytes = 30 * 1024 * 1024; // 30 MB

    public StockFinancialReviewsController(AppDbContext db, IStorageService storage)
    {
        _db      = db;
        _storage = storage;
    }

    [Route("")]
    public async Task<IActionResult> Index(int stockId)
    {
        var stock = await _db.Stocks.FindAsync(stockId);
        if (stock == null) return NotFound();

        var reviews = await _db.StockFinancialReviews
            .Include(r => r.StatementType)
            .Where(r => r.StockId == stockId)
            .OrderByDescending(r => r.ReviewDate)
            .ToListAsync();

        ViewBag.Stock = stock;
        return View("~/Views/Admin/StockFinancialReviews/Index.cshtml", reviews);
    }

    [Route("create")]
    public async Task<IActionResult> Create(int stockId)
    {
        var stock = await _db.Stocks.FindAsync(stockId);
        if (stock == null) return NotFound();

        return View("~/Views/Admin/StockFinancialReviews/Create.cshtml",
            await BuildFormVm(new StockFinancialReviewFormViewModel(), stock));
    }

    [HttpPost, Route("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int stockId, StockFinancialReviewFormViewModel vm)
    {
        var stock = await _db.Stocks.FindAsync(stockId);
        if (stock == null) return NotFound();

        if (!IsAllowedStatus(vm.ShariahStatus))
            ModelState.AddModelError(nameof(vm.ShariahStatus), "موقف السهم غير صالح");

        if (!ModelState.IsValid)
            return View("~/Views/Admin/StockFinancialReviews/Create.cshtml", await BuildFormVm(vm, stock));

        var review = new StockFinancialReview
        {
            StockId = stockId,
            FinancialStatementTypeId = vm.FinancialStatementTypeId,
            ReviewDate = vm.ReviewDate,
            ReviewYear = vm.ReviewYear,
            ProhibitedIncomeRatio = vm.ProhibitedIncomeRatio,
            NonShariahInvestmentRatio = vm.NonShariahInvestmentRatio,
            NonShariahObligationsRatio = vm.NonShariahObligationsRatio,
            LiquidAssetsRatio = vm.LiquidAssetsRatio,
            ShariahStatus = vm.ShariahStatus,
            PurityGrades = vm.SelectedPurityGrades.Aggregate(PurityGrade.None, (acc, v) => acc | (PurityGrade)v),
            Notes = vm.Notes,
            PdfUrl = await _storage.UploadAsync(vm.PdfFile!, "reviews", PdfOnly, PdfMaxBytes),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.StockFinancialReviews.Add(review);
        await _db.SaveChangesAsync();
        TempData["Success"] = "تم إضافة المراجعة بنجاح";
        return Redirect($"/admin/stocks/{stockId}/reviews");
    }

    [Route("edit/{id:int}")]
    public async Task<IActionResult> Edit(int stockId, int id)
    {
        var stock = await _db.Stocks.FindAsync(stockId);
        if (stock == null) return NotFound();

        var review = await _db.StockFinancialReviews.FindAsync(id);
        if (review == null || review.StockId != stockId) return NotFound();

        var vm = new StockFinancialReviewFormViewModel
        {
            Id = review.Id,
            StockId = stockId,
            FinancialStatementTypeId = review.FinancialStatementTypeId,
            ReviewDate = review.ReviewDate,
            ReviewYear = review.ReviewYear,
            ProhibitedIncomeRatio = review.ProhibitedIncomeRatio,
            NonShariahInvestmentRatio = review.NonShariahInvestmentRatio,
            NonShariahObligationsRatio = review.NonShariahObligationsRatio,
            LiquidAssetsRatio = review.LiquidAssetsRatio,
            ShariahStatus = review.ShariahStatus,
            Notes = review.Notes,
            ExistingPdfUrl = review.PdfUrl,
            SelectedPurityGrades = Enum.GetValues<PurityGrade>()
                .Where(g => g != PurityGrade.None && review.PurityGrades.HasFlag(g))
                .Select(g => (int)g).ToList()
        };
        return View("~/Views/Admin/StockFinancialReviews/Edit.cshtml", await BuildFormVm(vm, stock));
    }

    [HttpPost, Route("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int stockId, int id, StockFinancialReviewFormViewModel vm)
    {
        var stock = await _db.Stocks.FindAsync(stockId);
        if (stock == null) return NotFound();

        if (!IsAllowedStatus(vm.ShariahStatus))
            ModelState.AddModelError(nameof(vm.ShariahStatus), "موقف السهم غير صالح");

        if (!ModelState.IsValid)
            return View("~/Views/Admin/StockFinancialReviews/Edit.cshtml", await BuildFormVm(vm, stock));

        var review = await _db.StockFinancialReviews.FindAsync(id);
        if (review == null || review.StockId != stockId) return NotFound();

        review.FinancialStatementTypeId = vm.FinancialStatementTypeId;
        review.ReviewDate = vm.ReviewDate;
        review.ReviewYear = vm.ReviewYear;
        review.ProhibitedIncomeRatio = vm.ProhibitedIncomeRatio;
        review.NonShariahInvestmentRatio = vm.NonShariahInvestmentRatio;
        review.NonShariahObligationsRatio = vm.NonShariahObligationsRatio;
        review.LiquidAssetsRatio = vm.LiquidAssetsRatio;
        review.ShariahStatus = vm.ShariahStatus;
        review.PurityGrades = vm.SelectedPurityGrades.Aggregate(PurityGrade.None, (acc, v) => acc | (PurityGrade)v);
        review.Notes = vm.Notes;
        review.UpdatedAt = DateTime.UtcNow;

        if (vm.PdfFile != null)
        {
            await _storage.DeleteAsync(review.PdfUrl);
            review.PdfUrl = await _storage.UploadAsync(vm.PdfFile, "reviews", PdfOnly, PdfMaxBytes);
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "تم تحديث المراجعة بنجاح";
        return Redirect($"/admin/stocks/{stockId}/reviews");
    }

    [HttpPost, Route("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int stockId, int id)
    {
        var review = await _db.StockFinancialReviews.FindAsync(id);
        if (review != null && review.StockId == stockId)
        {
            await _storage.DeleteAsync(review.PdfUrl);
            _db.StockFinancialReviews.Remove(review);
            await _db.SaveChangesAsync();
        }
        TempData["Success"] = "تم حذف المراجعة";
        return Redirect($"/admin/stocks/{stockId}/reviews");
    }

    private static bool IsAllowedStatus(ShariahStatus s) =>
        s == ShariahStatus.Halal || s == ShariahStatus.MustPurify || s == ShariahStatus.StatusBlocked;

    private async Task<StockFinancialReviewFormViewModel> BuildFormVm(
        StockFinancialReviewFormViewModel vm, Stock stock)
    {
        vm.StockId = stock.Id;
        vm.StockName = stock.NameAr;
        var types = await _db.FinancialStatementTypes
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync();
        vm.StatementTypes = types.Select(t =>
            new SelectListItem(t.NameAr, t.Id.ToString()));
        return vm;
    }
}
