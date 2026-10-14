using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HalalStocks.Controllers;

[Authorize]
[Route("recommendations/wallet")]
public class InvestmentWalletController : Controller
{
    private readonly IInvestmentWalletService _walletService;
    private readonly IRecommendationService _recommendationService;
    private readonly IUserRecommendationService _userRecommendationService;
    private readonly IRecommendationRequestService _requestService;
    private readonly IWalletManagementRequestService _managementRequestService;
    private readonly UserManager<AppUser> _userManager;

    public InvestmentWalletController(
        IInvestmentWalletService walletService,
        IRecommendationService recommendationService,
        IUserRecommendationService userRecommendationService,
        IRecommendationRequestService requestService,
        IWalletManagementRequestService managementRequestService,
        UserManager<AppUser> userManager)
    {
        _walletService = walletService;
        _recommendationService = recommendationService;
        _userRecommendationService = userRecommendationService;
        _requestService = requestService;
        _managementRequestService = managementRequestService;
        _userManager = userManager;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return View("~/Views/Recommendations/WalletGuestGate.cshtml");

        var pageState = await ResolvePageStateAsync(user);
        ViewBag.PageState = pageState;
        if (pageState != RecommendationsPageState.List)
            return View("~/Views/Recommendations/Wallet.cshtml", new WalletItemFormViewModel());

        var vm = await BuildViewModelAsync(user, null);
        ViewBag.Wallet = vm.Wallet;
        ViewBag.Summary = vm.Wallet == null ? (0m, 0m, 0m, 0m, 0m) : await _walletService.GetSummaryAsync(user.Id);
        ViewBag.ManagementStatus = (await _managementRequestService.GetByUserIdAsync(user.Id))?.Status;
        return View("~/Views/Recommendations/Wallet.cshtml", vm);
    }

    [HttpGet("items/{id:int}/edit")]
    public async Task<IActionResult> EditItem(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var pageState = await ResolvePageStateAsync(user);
        ViewBag.PageState = pageState;
        if (pageState != RecommendationsPageState.List)
            return View("~/Views/Recommendations/Wallet.cshtml", new WalletItemFormViewModel());

        var wallet = await _walletService.GetByUserIdAsync(user.Id);
        var item = wallet?.Items.FirstOrDefault(i => i.Id == id);
        if (item == null) return NotFound();

        if (wallet!.MaxRiskPercent == null)
        {
            TempData["Error"] = "يرجى تحديد نسبة المخاطر المقبولة أولاً قبل تعديل أسهم محفظتك";
            return RedirectToAction(nameof(Index));
        }

        var vm = await BuildViewModelAsync(user, item);
        ViewBag.Wallet = wallet;
        ViewBag.Summary = wallet == null ? (0m, 0m, 0m, 0m, 0m) : await _walletService.GetSummaryAsync(user.Id);
        ViewBag.ManagementStatus = (await _managementRequestService.GetByUserIdAsync(user.Id))?.Status;
        return View("~/Views/Recommendations/Wallet.cshtml", vm);
    }

    [HttpPost("management-request")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestManagement(bool consent)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (await ResolvePageStateAsync(user) != RecommendationsPageState.List)
            return RedirectToAction(nameof(Index));

        if (!consent)
        {
            TempData["Error"] = "يجب الموافقة على السماح للمختص بمساعدتك في إدارة محفظتك";
            return RedirectToAction(nameof(Index));
        }

        var existing = await _managementRequestService.GetByUserIdAsync(user.Id);
        if (existing?.Status is WalletManagementRequestStatus.Pending or WalletManagementRequestStatus.Approved)
            return RedirectToAction(nameof(Index));

        await _managementRequestService.UpsertPendingRequestAsync(user.Id);
        TempData["Success"] = "تم إرسال طلب المساعدة في إدارة المحفظة";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("cash")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCashBalance(InvestmentWalletFormViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "يرجى إدخال قيمة صالحة للسيولة";
            return RedirectToAction(nameof(Index));
        }

        var newCashBalance = vm.CashBalance ?? 0;
        var wallet = await _walletService.GetByUserIdAsync(user.Id);
        if (wallet?.MaxRiskPercent != null)
        {
            var totalExpectedLoss = wallet.Items.Sum(i => i.ExpectedLoss);
            var projectedRiskPercent = ExpectedRiskPercent(newCashBalance, totalExpectedLoss);
            if (projectedRiskPercent > wallet.MaxRiskPercent.Value)
            {
                TempData["Error"] = "لا يمكن تخفيض السيولة إلى هذه القيمة لأن ذلك سيجعل نسبة المخاطر المتوقعة لمحفظتك تتجاوز الحد الأقصى الذي حددته لنسبة المخاطر";
                return RedirectToAction(nameof(Index));
            }
        }

        try
        {
            await _walletService.CreateOrUpdateCashBalanceAsync(user.Id, newCashBalance);
            TempData["Success"] = "تم تحديث السيولة بنجاح";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("risk")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRiskPercent(WalletRiskFormViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "يرجى إدخال نسبة مخاطر صالحة بين 0 و 100";
            return RedirectToAction(nameof(Index));
        }

        var wallet = await _walletService.GetByUserIdAsync(user.Id);
        var totalExpectedLoss = wallet?.Items.Sum(i => i.ExpectedLoss) ?? 0;
        var actualRiskPercent = ExpectedRiskPercent(wallet?.CashBalance ?? 0, totalExpectedLoss);

        if (vm.RiskPercent!.Value < actualRiskPercent)
        {
            TempData["Error"] = "لا يمكن أن تكون نسبة المخاطر التي تدخلها أقل من نسبة المخاطر المتوقعة الحالية لمحفظتك";
            return RedirectToAction(nameof(Index));
        }

        await _walletService.SetMaxRiskPercentAsync(user.Id, vm.RiskPercent.Value);
        TempData["Success"] = "تم تحديث نسبة المخاطر بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("items")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveItem(WalletItemFormViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var wallet = await _walletService.GetByUserIdAsync(user.Id);
        if (wallet == null)
        {
            TempData["Error"] = "يرجى تحديد السيولة أولاً";
            return RedirectToAction(nameof(Index));
        }

        if (wallet.MaxRiskPercent == null)
        {
            TempData["Error"] = "يرجى تحديد نسبة المخاطر المقبولة أولاً قبل إضافة أسهم إلى محفظتك";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            var invalidVm = await BuildViewModelAsync(user, vm.Id.HasValue ? wallet.Items.FirstOrDefault(i => i.Id == vm.Id.Value) : null);
            invalidVm.Id = vm.Id;
            invalidVm.StockId = vm.StockId;
            invalidVm.Quantity = vm.Quantity;
            invalidVm.PurchasePrice = vm.PurchasePrice;
            invalidVm.TargetPrice = vm.TargetPrice;
            invalidVm.StopLossPrice = vm.StopLossPrice;
            invalidVm.PurchasePriceSource = vm.PurchasePriceSource;
            invalidVm.TargetPriceSource = vm.TargetPriceSource;
            invalidVm.StopLossSource = vm.StopLossSource;
            ViewBag.Wallet = wallet;
            ViewBag.Summary = await _walletService.GetSummaryAsync(user.Id);
            return View("~/Views/Recommendations/Wallet.cshtml", invalidVm);
        }

        if (vm.Quantity is null || vm.PurchasePrice is null || vm.TargetPrice is null || vm.StopLossPrice is null || vm.StockId is null)
        {
            TempData["Error"] = "يرجى مراجعة الحقول المطلوبة";
            return RedirectToAction(nameof(Index));
        }

        vm.PurchasePriceSource ??= "ClosePrice";
        vm.TargetPriceSource ??= "Resistance1";
        vm.StopLossSource ??= "Support1";

        var currentInvested = wallet.Items.Where(i => !vm.Id.HasValue || i.Id != vm.Id.Value).Sum(i => i.TotalPurchase);
        var availableCash = wallet.CashBalance - currentInvested;
        var totalPurchase = (vm.Quantity ?? 0) * (vm.PurchasePrice ?? 0);
        if (totalPurchase > availableCash)
        {
            ModelState.AddModelError(nameof(vm.PurchasePrice), "لا يمكن أن يتجاوز اجمالي الشراء السيولة الحالية");
            var invalidVm = await BuildViewModelAsync(user, vm.Id.HasValue ? wallet.Items.FirstOrDefault(i => i.Id == vm.Id.Value) : null);
            invalidVm.Id = vm.Id;
            invalidVm.StockId = vm.StockId;
            invalidVm.Quantity = vm.Quantity;
            invalidVm.PurchasePrice = vm.PurchasePrice;
            invalidVm.TargetPrice = vm.TargetPrice;
            invalidVm.StopLossPrice = vm.StopLossPrice;
            invalidVm.PurchasePriceSource = vm.PurchasePriceSource;
            invalidVm.TargetPriceSource = vm.TargetPriceSource;
            invalidVm.StopLossSource = vm.StopLossSource;
            ViewBag.Wallet = wallet;
            ViewBag.Summary = await _walletService.GetSummaryAsync(user.Id);
            return View("~/Views/Recommendations/Wallet.cshtml", invalidVm);
        }

        if ((vm.TargetPrice ?? 0) <= (vm.PurchasePrice ?? 0))
        {
            ModelState.AddModelError(nameof(vm.TargetPrice), "يجب أن يكون المستهدف أعلى من سعر الشراء");
            var invalidVm = await BuildViewModelAsync(user, vm.Id.HasValue ? wallet.Items.FirstOrDefault(i => i.Id == vm.Id.Value) : null);
            invalidVm.Id = vm.Id;
            invalidVm.StockId = vm.StockId;
            invalidVm.Quantity = vm.Quantity;
            invalidVm.PurchasePrice = vm.PurchasePrice;
            invalidVm.TargetPrice = vm.TargetPrice;
            invalidVm.StopLossPrice = vm.StopLossPrice;
            invalidVm.PurchasePriceSource = vm.PurchasePriceSource;
            invalidVm.TargetPriceSource = vm.TargetPriceSource;
            invalidVm.StopLossSource = vm.StopLossSource;
            ViewBag.Wallet = wallet;
            ViewBag.Summary = await _walletService.GetSummaryAsync(user.Id);
            return View("~/Views/Recommendations/Wallet.cshtml", invalidVm);
        }

        if ((vm.StopLossPrice ?? 0) >= (vm.PurchasePrice ?? 0))
        {
            ModelState.AddModelError(nameof(vm.StopLossPrice), "يجب أن يكون وقف الخسارة أقل من سعر الشراء");
            var invalidVm = await BuildViewModelAsync(user, vm.Id.HasValue ? wallet.Items.FirstOrDefault(i => i.Id == vm.Id.Value) : null);
            invalidVm.Id = vm.Id;
            invalidVm.StockId = vm.StockId;
            invalidVm.Quantity = vm.Quantity;
            invalidVm.PurchasePrice = vm.PurchasePrice;
            invalidVm.TargetPrice = vm.TargetPrice;
            invalidVm.StopLossPrice = vm.StopLossPrice;
            invalidVm.PurchasePriceSource = vm.PurchasePriceSource;
            invalidVm.TargetPriceSource = vm.TargetPriceSource;
            invalidVm.StopLossSource = vm.StopLossSource;
            ViewBag.Wallet = wallet;
            ViewBag.Summary = await _walletService.GetSummaryAsync(user.Id);
            return View("~/Views/Recommendations/Wallet.cshtml", invalidVm);
        }

        var incomingExpectedLoss = vm.Quantity.Value * (vm.PurchasePrice.Value - vm.StopLossPrice.Value);
        var otherItemsExpectedLoss = wallet.Items.Where(i => !vm.Id.HasValue || i.Id != vm.Id.Value).Sum(i => i.ExpectedLoss);
        var projectedExpectedRiskPercent = ExpectedRiskPercent(wallet.CashBalance, otherItemsExpectedLoss + incomingExpectedLoss);
        if (projectedExpectedRiskPercent > wallet.MaxRiskPercent!.Value)
        {
            TempData["Error"] = "لا يمكن إضافة/تعديل هذا السهم لأن ذلك سيجعل نسبة المخاطر المتوقعة لمحفظتك تتجاوز الحد الأقصى الذي حددته لنسبة المخاطر";
            return RedirectToAction(nameof(Index));
        }

        var targetProfitPercent = vm.PurchasePrice.Value == 0 ? 0 : (vm.TargetPrice.Value - vm.PurchasePrice.Value) / vm.PurchasePrice.Value * 100m;
        var expectedLossPercent = vm.PurchasePrice.Value == 0 ? 0 : (vm.PurchasePrice.Value - vm.StopLossPrice.Value) / vm.PurchasePrice.Value * 100m;

        if (vm.Id.HasValue)
        {
            var existing = wallet.Items.FirstOrDefault(i => i.Id == vm.Id.Value);
            if (existing == null) return NotFound();

            existing.StockId = vm.StockId.Value;
            existing.Quantity = vm.Quantity.Value;
            existing.PurchasePrice = vm.PurchasePrice.Value;
            existing.PurchasePriceSource = vm.PurchasePriceSource!;
            existing.TotalPurchase = totalPurchase;
            existing.RelativeWeightPercent = wallet.CashBalance == 0 ? 0 : totalPurchase / wallet.CashBalance * 100m;
            existing.TargetPrice = vm.TargetPrice.Value;
            existing.TargetPriceSource = vm.TargetPriceSource!;
            existing.StopLossPrice = vm.StopLossPrice.Value;
            existing.StopLossSource = vm.StopLossSource!;
            existing.ExpectedProfit = vm.Quantity.Value * (vm.TargetPrice.Value - vm.PurchasePrice.Value);
            existing.ExpectedLoss = vm.Quantity.Value * (vm.PurchasePrice.Value - vm.StopLossPrice.Value);
            existing.TargetProfitPercent = targetProfitPercent;
            existing.ExpectedLossPercent = expectedLossPercent;
            existing.UpdatedAt = DateTime.UtcNow;
            await _walletService.UpdateItemAsync(existing);
            TempData["Success"] = "تم تحديث السهم في المحفظة";
        }
        else
        {
            var item = new WalletItem
            {
                WalletId = wallet.Id,
                StockId = vm.StockId.Value,
                Quantity = vm.Quantity.Value,
                PurchasePrice = vm.PurchasePrice.Value,
                PurchasePriceSource = vm.PurchasePriceSource!,
                TotalPurchase = totalPurchase,
                RelativeWeightPercent = wallet.CashBalance == 0 ? 0 : totalPurchase / wallet.CashBalance * 100m,
                TargetPrice = vm.TargetPrice.Value,
                TargetPriceSource = vm.TargetPriceSource!,
                StopLossPrice = vm.StopLossPrice.Value,
                StopLossSource = vm.StopLossSource!,
                ExpectedProfit = vm.Quantity.Value * (vm.TargetPrice.Value - vm.PurchasePrice.Value),
                ExpectedLoss = vm.Quantity.Value * (vm.PurchasePrice.Value - vm.StopLossPrice.Value),
                TargetProfitPercent = targetProfitPercent,
                ExpectedLossPercent = expectedLossPercent
            };

            await _walletService.CreateItemAsync(item);
            TempData["Success"] = "تمت إضافة السهم إلى محفظتك";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("items/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var item = await _walletService.GetItemByIdAsync(id);
        if (item == null) return NotFound();

        var wallet = await _walletService.GetByUserIdAsync(user.Id);
        if (wallet == null || item.WalletId != wallet.Id) return Forbid();

        await _walletService.DeleteItemAsync(id);
        TempData["Success"] = "تم حذف العنصر";
        return RedirectToAction(nameof(Index));
    }

    private async Task<WalletItemFormViewModel> BuildViewModelAsync(AppUser user, WalletItem? editingItem)
    {
        var userId = user.Id;
        var wallet = await _walletService.GetByUserIdAsync(userId);
        var generalRecs = (await _recommendationService.GetAllAsync())
            .Where(r => r.Stock != null && !r.IsHidden)
            .ToList();
        var privateRecs = (await _userRecommendationService.GetByUserIdAsync(userId)).Where(r => r.Stock != null).ToList();

        // Every recommended stock stays selectable at all times — a stock already in the wallet
        // can be bought again as a separate tranche.
        var pickable = new Dictionary<int, RecommendationBase>();
        foreach (var r in generalRecs)
            pickable[r.StockId] = r;
        foreach (var r in privateRecs)
            if (!pickable.ContainsKey(r.StockId)) pickable[r.StockId] = r;

        var pickableList = pickable.Values.OrderBy(r => r.Stock.TickerSymbol).ToList();

        var vm = new WalletItemFormViewModel
        {
            Id = editingItem?.Id,
            RecommendationStocks = pickableList,
            Wallet = wallet,
            CashBalance = wallet?.CashBalance ?? 0,
            CurrentAvailableCash = wallet?.CashBalance ?? 0,
            CurrentTotalPurchase = 0
        };

        if (editingItem != null)
        {
            vm.StockId = editingItem.StockId;
            vm.Quantity = editingItem.Quantity;
            vm.PurchasePrice = editingItem.PurchasePrice;
            vm.TargetPrice = editingItem.TargetPrice;
            vm.StopLossPrice = editingItem.StopLossPrice;
            vm.PurchasePriceSource = editingItem.PurchasePriceSource ?? "ClosePrice";
            vm.TargetPriceSource = editingItem.TargetPriceSource ?? "Resistance1";
            vm.StopLossSource = editingItem.StopLossSource ?? "Support1";
        }

        return vm;
    }

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

    // Expected-risk % = total expected loss across wallet items, as a % of cash balance.
    private static decimal ExpectedRiskPercent(decimal cashBalance, decimal totalExpectedLoss) =>
        cashBalance <= 0 ? 0 : totalExpectedLoss / cashBalance * 100m;
}
