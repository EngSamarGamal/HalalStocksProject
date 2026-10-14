using HalalStocks.Helpers;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HalalStocks.Controllers;

[Authorize]
[Route("recommendations/portfolio")]
public class RealPortfolioController : Controller
{
    private readonly IRealPortfolioService _portfolioService;
    private readonly IRealPortfolioViewModelBuilder _viewModelBuilder;
    private readonly ITradingViewPriceService _priceService;
    private readonly IStockService _stockService;
    private readonly IRecommendationRequestService _requestService;
    private readonly IWalletManagementRequestService _managementRequestService;
    private readonly UserManager<AppUser> _userManager;

    public RealPortfolioController(
        IRealPortfolioService portfolioService,
        IRealPortfolioViewModelBuilder viewModelBuilder,
        ITradingViewPriceService priceService,
        IStockService stockService,
        IRecommendationRequestService requestService,
        IWalletManagementRequestService managementRequestService,
        UserManager<AppUser> userManager)
    {
        _portfolioService         = portfolioService;
        _viewModelBuilder         = viewModelBuilder;
        _priceService             = priceService;
        _stockService             = stockService;
        _requestService           = requestService;
        _managementRequestService = managementRequestService;
        _userManager              = userManager;
    }

    [AllowAnonymous]
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return View("~/Views/RealPortfolio/GuestGate.cshtml");

        var pageState = await ResolvePageStateAsync(user);
        if (pageState != RecommendationsPageState.List)
            return View(new RealPortfolioIndexViewModel { PageState = pageState });

        await SetManagementStatusAsync(user.Id);
        return View(await _viewModelBuilder.BuildAsync(user.Id, null));
    }

    [HttpGet("items/{id:int}/edit")]
    public async Task<IActionResult> EditItem(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var pageState = await ResolvePageStateAsync(user);
        if (pageState != RecommendationsPageState.List)
            return View("Index", new RealPortfolioIndexViewModel { PageState = pageState });

        var portfolio = await _portfolioService.GetByUserIdAsync(user.Id);
        var item = portfolio?.Items.FirstOrDefault(i => i.Id == id);
        if (item == null) return NotFound();

        await SetManagementStatusAsync(user.Id);
        return View("Index", await _viewModelBuilder.BuildAsync(user.Id, item));
    }

    [HttpPost("capital")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCapital(decimal? capital)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (await ResolvePageStateAsync(user) != RecommendationsPageState.List)
            return RedirectToAction(nameof(Index));

        if (capital is null || capital < 0)
        {
            TempData["PortfolioError"] ="يرجى إدخال قيمة صالحة لرأس المال";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _portfolioService.CreateOrUpdateCapitalAsync(user.Id, capital.Value);
            TempData["Success"] = "تم تحديث رأس المال بنجاح";
        }
        catch (InvalidOperationException ex)
        {
            TempData["PortfolioError"] =ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // The form posts nested under the index view model, so binding and every ModelState key
    // carry the "Form." prefix — see AddFormError below.
    private const string FormPrefix = "Form";

    [HttpPost("items")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveItem([Bind(Prefix = FormPrefix)] RealPortfolioItemFormViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (await ResolvePageStateAsync(user) != RecommendationsPageState.List)
            return RedirectToAction(nameof(Index));

        var portfolio = await _portfolioService.GetByUserIdAsync(user.Id);
        if (portfolio == null || portfolio.Capital <= 0)
        {
            TempData["PortfolioError"] ="يرجى تحديد رأس المال أولاً قبل إضافة أسهم إلى محفظتك الفعلية";
            return RedirectToAction(nameof(Index));
        }

        var editingItem = vm.Id.HasValue ? portfolio.Items.FirstOrDefault(i => i.Id == vm.Id.Value) : null;
        if (vm.Id.HasValue && editingItem == null) return NotFound();

        if (vm.PurchaseDate > DateOnly.FromDateTime(DateTime.Today))
            AddFormError(nameof(vm.PurchaseDate), "لا يمكن أن يكون تاريخ الشراء في المستقبل");

        if (vm.StockId.HasValue && await _stockService.GetByIdAsync(vm.StockId.Value) is null)
            AddFormError(nameof(vm.StockId), "السهم المختار غير موجود");

        var totalPurchase = (vm.Quantity ?? 0) * (vm.PurchasePrice ?? 0);
        var investedByOthers = portfolio.Items
            .Where(i => editingItem == null || i.Id != editingItem.Id)
            .Sum(i => i.TotalPurchase);
        var availableCash = portfolio.Capital - investedByOthers;

        if (ModelState.IsValid && totalPurchase > availableCash)
            AddFormError(nameof(vm.PurchasePrice),
                $"لا يمكن أن تتجاوز القيمة الشرائية السيولة المتاحة ({availableCash:N2})");

        if (!ModelState.IsValid)
        {
            var invalidVm = await _viewModelBuilder.BuildAsync(user.Id, editingItem);
            invalidVm.Form = await _viewModelBuilder.HydrateFormAsync(vm);
            await SetManagementStatusAsync(user.Id);
            return View("Index", invalidVm);
        }

        var weightPercent = portfolio.Capital == 0 ? 0 : totalPurchase / portfolio.Capital * 100m;

        if (editingItem != null)
        {
            editingItem.StockId = vm.StockId!.Value;
            editingItem.PurchaseDate = vm.PurchaseDate!.Value;
            editingItem.Quantity = vm.Quantity!.Value;
            editingItem.PurchasePrice = vm.PurchasePrice!.Value;
            editingItem.TotalPurchase = totalPurchase;
            editingItem.PurchaseWeightPercent = weightPercent;
            await _portfolioService.UpdateItemAsync(editingItem);
            TempData["Success"] = "تم تحديث السهم في المحفظة الفعلية";
        }
        else
        {
            await _portfolioService.CreateItemAsync(new RealPortfolioItem
            {
                PortfolioId = portfolio.Id,
                StockId = vm.StockId!.Value,
                PurchaseDate = vm.PurchaseDate!.Value,
                Quantity = vm.Quantity!.Value,
                PurchasePrice = vm.PurchasePrice!.Value,
                TotalPurchase = totalPurchase,
                PurchaseWeightPercent = weightPercent
            });
            TempData["Success"] = "تمت إضافة السهم إلى محفظتك الفعلية";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("items/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var item = await _portfolioService.GetItemByIdAsync(id);
        if (item == null) return NotFound();

        var portfolio = await _portfolioService.GetByUserIdAsync(user.Id);
        if (portfolio == null || item.PortfolioId != portfolio.Id) return Forbid();

        await _portfolioService.DeleteItemAsync(id);
        TempData["Success"] = "تم حذف السهم من المحفظة الفعلية";
        return RedirectToAction(nameof(Index));
    }

    // ── JSON endpoints backing the stock picker ───────────────────────────────

    [HttpGet("/api/stocks/search")]
    public async Task<IActionResult> SearchStocks(string? q)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        // Explicit 403 rather than Forbid(): cookie auth turns Forbid() into a 302 to the
        // access-denied page, which fetch() would then try to parse as JSON.
        if (await ResolvePageStateAsync(user) != RecommendationsPageState.List)
            return StatusCode(StatusCodes.Status403Forbidden);

        var stocks = await _stockService.SearchActiveStocksAsync(q);
        return Json(stocks.Select(s => new
        {
            id           = s.Id,
            ticker       = s.TickerSymbol,
            nameAr       = s.NameAr,
            sectorNameAr = s.Category?.NameAr ?? string.Empty,
            shariahLabel = ShariahStatusHelper.Format(s.ShariahStatus)
        }));
    }

    [HttpGet("/api/stocks/{id:int}/quote")]
    public async Task<IActionResult> StockQuote(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        // Explicit 403 rather than Forbid(): cookie auth turns Forbid() into a 302 to the
        // access-denied page, which fetch() would then try to parse as JSON.
        if (await ResolvePageStateAsync(user) != RecommendationsPageState.List)
            return StatusCode(StatusCodes.Status403Forbidden);

        var stock = await _stockService.GetByIdAsync(id);
        if (stock == null) return NotFound();

        var marketPrice = await _priceService.GetPriceAsync(stock.TickerSymbol);

        return Json(new
        {
            id           = stock.Id,
            ticker       = stock.TickerSymbol,
            nameAr       = stock.NameAr,
            sectorNameAr = stock.Category?.NameAr ?? string.Empty,
            shariahLabel = ShariahStatusHelper.Format(stock.ShariahStatus),
            marketPrice
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void AddFormError(string propertyName, string message) =>
        ModelState.AddModelError($"{FormPrefix}.{propertyName}", message);

    // The wallet-management request covers all three tabs, so its banner renders on this page too.
    private async Task SetManagementStatusAsync(string userId) =>
        ViewBag.ManagementStatus = (await _managementRequestService.GetByUserIdAsync(userId))?.Status;

    private async Task<RecommendationsPageState> ResolvePageStateAsync(AppUser user)
    {
        if (IsAdminUser(user)) return RecommendationsPageState.List;

        var request = await _requestService.GetByUserIdAsync(user.Id);
        if (request?.Status == RecommendationRequestStatus.Approved) return RecommendationsPageState.List;
        if (request?.Status == RecommendationRequestStatus.Pending) return RecommendationsPageState.Pending;
        return RecommendationsPageState.Gate;
    }

    private static bool IsAdminUser(AppUser user) =>
        user.Role is UserRole.SuperAdmin or UserRole.Admin or UserRole.Editor;
}
