using HalalStocks.Data;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Controllers.Admin;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
[Route("admin")]
public class DashboardController : Controller
{
    private readonly AppDbContext      _db;
    private readonly IUserService      _userService;
    private readonly IGoldPriceService _gold;

    public DashboardController(AppDbContext db, IUserService userService, IGoldPriceService gold)
    {
        _db          = db;
        _userService = userService;
        _gold        = gold;
    }

    [Route("")]
    [Route("dashboard")]
    public async Task<IActionResult> Index()
    {
        var vm = new DashboardViewModel
        {
            TotalStocks       = await _db.Stocks.CountAsync(s => s.IsActive),
            TotalCategories   = await _db.StockCategories.CountAsync(),
            TotalArticles     = await _db.Articles.CountAsync(),
            TotalUsers        = await _userService.GetCountAsync(),
            TotalZakatRecords        = await _db.ZakatRecords.CountAsync(),
            NewPortfolioRequests     = await _db.PortfolioRequests.CountAsync(r => r.Status == HalalStocks.Models.PortfolioRequestStatus.New)
        };

        try
        {
            var snapshot = await _gold.GetSnapshotAsync();
            vm.GoldPrice21K       = snapshot?.PriceGram21K;
            vm.GoldPriceFetchedAt = snapshot?.FetchedAt.ToLocalTime();
            vm.GoldPriceDate      = snapshot?.PriceDate;
            vm.GoldPriceSource    = snapshot?.Source;
        }
        catch
        {
            // Gold price table may not be migrated yet — show dashboard without it
        }

        return View("~/Views/Admin/Dashboard/Index.cshtml", vm);
    }

    [HttpPost("gold-price/refresh")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshGoldPrice()
    {
        try
        {
            var price = await _gold.ForceRefreshAsync();
            TempData[price > 0 ? "Success" : "Error"] = price > 0
                ? $"تم تحديث سعر الذهب: {price:N2} ج.م"
                : "تعذر جلب سعر الذهب من API — تحقق من صحة المفتاح في appsettings.json.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"خطأ: {ex.GetType().Name} — {ex.Message}";
        }
        return Redirect("/admin");
    }
}
