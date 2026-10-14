using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HalalStocks.Controllers.Admin;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
[Route("admin/stocks")]
public class StocksController : Controller
{
    private readonly IStockService _stockService;
    public StocksController(IStockService stockService) => _stockService = stockService;

    [Route("")]
    public async Task<IActionResult> Index(string? search, int? categoryId, ShariahStatus? status, int page = 1)
    {
        var categoryIds = categoryId.HasValue ? new List<int> { categoryId.Value } : null;
        var (stocks, total) = await _stockService.GetStocksAsync(search, categoryIds, status, null, page, 20, includeInactive: true); // indexIds: null = no filter
        var categories = await _stockService.GetCategoriesAsync();
        ViewBag.Categories = categories;
        ViewBag.Search = search;
        ViewBag.CategoryId = categoryId;
        ViewBag.Status = status;
        ViewBag.TotalPages = (int)Math.Ceiling(total / 20.0);
        ViewBag.CurrentPage = page;
        return View("~/Views/Admin/Stocks/Index.cshtml", stocks);
    }

    [Route("create")]
    public async Task<IActionResult> Create()
    {
        return View("~/Views/Admin/Stocks/Create.cshtml", await BuildFormVm(new StockFormViewModel()));
    }

    [HttpPost, Route("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StockFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/Stocks/Create.cshtml", await BuildFormVm(vm));

        var stock = new Stock
        {
            TickerSymbol = vm.TickerSymbol,
            NameAr = vm.NameAr,
            NameEn = vm.NameEn,
            CategoryId = vm.CategoryId,
            Exchange = vm.Exchange,
            ShariahStatus = vm.ShariahStatus,
            PurificationRatio = vm.PurificationRatio,
            Notes = vm.Notes,
            IsActive = vm.IsActive,
            ProhibitedIncomeRatio = vm.ProhibitedIncomeRatio,
            NonShariahInvestmentRatio = vm.NonShariahInvestmentRatio,
            NonShariahObligationsRatio = vm.NonShariahObligationsRatio,
            LiquidAssetsRatio = vm.LiquidAssetsRatio,
            LastReviewedAt = vm.LastReviewedAt,
            AdditionalNotes = vm.AdditionalNotes,
            TotalCompanyShares = vm.TotalCompanyShares,
            TotalCompanyAssets = vm.TotalCompanyAssets,
            PurityGrades = vm.SelectedPurityGrades.Aggregate(PurityGrade.None, (acc, v) => acc | (PurityGrade)v)
        };
        await _stockService.CreateAsync(stock, vm.SelectedIndexIds);
        TempData["Success"] = "تم إضافة السهم بنجاح";
        return Redirect("/admin/stocks");
    }

    [Route("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var stock = await _stockService.GetByIdAsync(id);
        if (stock == null) return NotFound();
        var vm = new StockFormViewModel
        {
            Id = stock.Id,
            TickerSymbol = stock.TickerSymbol,
            NameAr = stock.NameAr,
            NameEn = stock.NameEn,
            CategoryId = stock.CategoryId,
            Exchange = stock.Exchange,
            ShariahStatus = stock.ShariahStatus,
            PurificationRatio = stock.PurificationRatio,
            Notes = stock.Notes,
            IsActive = stock.IsActive,
            ProhibitedIncomeRatio = stock.ProhibitedIncomeRatio,
            NonShariahInvestmentRatio = stock.NonShariahInvestmentRatio,
            NonShariahObligationsRatio = stock.NonShariahObligationsRatio,
            LiquidAssetsRatio = stock.LiquidAssetsRatio,
            LastReviewedAt = stock.LastReviewedAt,
            AdditionalNotes = stock.AdditionalNotes,
            TotalCompanyShares = stock.TotalCompanyShares,
            TotalCompanyAssets = stock.TotalCompanyAssets,
            SelectedIndexIds = stock.Indices.Select(i => i.Id).ToList(),
            SelectedPurityGrades = Enum.GetValues<PurityGrade>()
                .Where(g => g != PurityGrade.None && stock.PurityGrades.HasFlag(g))
                .Select(g => (int)g).ToList()
        };
        return View("~/Views/Admin/Stocks/Edit.cshtml", await BuildFormVm(vm));
    }

    [HttpPost, Route("edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StockFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/Stocks/Edit.cshtml", await BuildFormVm(vm));

        var stock = await _stockService.GetByIdAsync(id);
        if (stock == null) return NotFound();
        stock.TickerSymbol = vm.TickerSymbol;
        stock.NameAr = vm.NameAr;
        stock.NameEn = vm.NameEn;
        stock.CategoryId = vm.CategoryId;
        stock.Exchange = vm.Exchange;
        stock.ShariahStatus = vm.ShariahStatus;
        stock.PurificationRatio = vm.PurificationRatio;
        stock.Notes = vm.Notes;
        stock.IsActive = vm.IsActive;
        stock.ProhibitedIncomeRatio = vm.ProhibitedIncomeRatio;
        stock.NonShariahInvestmentRatio = vm.NonShariahInvestmentRatio;
        stock.NonShariahObligationsRatio = vm.NonShariahObligationsRatio;
        stock.LiquidAssetsRatio = vm.LiquidAssetsRatio;
        stock.LastReviewedAt = vm.LastReviewedAt;
        stock.AdditionalNotes = vm.AdditionalNotes;
        stock.TotalCompanyShares = vm.TotalCompanyShares;
        stock.TotalCompanyAssets = vm.TotalCompanyAssets;
        stock.PurityGrades = vm.SelectedPurityGrades.Aggregate(PurityGrade.None, (acc, v) => acc | (PurityGrade)v);
        await _stockService.UpdateAsync(stock, vm.SelectedIndexIds);
        TempData["Success"] = "تم تحديث السهم بنجاح";
        return Redirect("/admin/stocks");
    }

    [HttpPost, Route("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _stockService.DeleteAsync(id);
        TempData["Success"] = "تم حذف السهم";
        return Redirect("/admin/stocks");
    }

    private async Task<StockFormViewModel> BuildFormVm(StockFormViewModel vm)
    {
        var categories = await _stockService.GetCategoriesAsync();
        vm.Categories = categories.Select(c => new SelectListItem(c.NameAr, c.Id.ToString()));

        var indices = await _stockService.GetIndicesAsync();
        vm.AvailableIndices = indices.Select(i => new SelectListItem(
            string.IsNullOrEmpty(i.NameEn) ? i.NameAr : $"{i.NameAr} ({i.NameEn})",
            i.Id.ToString()));

        return vm;
    }
}
