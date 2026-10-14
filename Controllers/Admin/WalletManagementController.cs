using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HalalStocks.Controllers.Admin;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
[Route("admin/wallet-management/{userId}")]
public class WalletManagementController : Controller
{
    private readonly IInvestmentWalletService _walletService;
    private readonly IRecommendationService _recommendationService;
    private readonly IUserRecommendationService _userRecommendationService;
    private readonly IWalletManagementRequestService _managementRequestService;
    private readonly IRealPortfolioService _portfolioService;
    private readonly IRealPortfolioViewModelBuilder _portfolioViewModelBuilder;
    private readonly IStockService _stockService;
    private readonly UserManager<AppUser> _userManager;

    public WalletManagementController(
        IInvestmentWalletService walletService,
        IRecommendationService recommendationService,
        IUserRecommendationService userRecommendationService,
        IWalletManagementRequestService managementRequestService,
        IRealPortfolioService portfolioService,
        IRealPortfolioViewModelBuilder portfolioViewModelBuilder,
        IStockService stockService,
        UserManager<AppUser> userManager)
    {
        _walletService = walletService;
        _recommendationService = recommendationService;
        _userRecommendationService = userRecommendationService;
        _managementRequestService = managementRequestService;
        _portfolioService = portfolioService;
        _portfolioViewModelBuilder = portfolioViewModelBuilder;
        _stockService = stockService;
        _userManager = userManager;
    }

    [HttpGet("recommendations")]
    public async Task<IActionResult> Recommendations(string userId)
    {
        var target = await ResolveTargetAsync(userId);
        if (target == null) return RedirectToRequests();

        var vm = new RecommendationsIndexViewModel
        {
            PageState = RecommendationsPageState.List,
            IsAdmin = false,
            Recommendations = (await _recommendationService.GetAllAsync()).Where(r => !r.IsHidden).ToList(),
            PrivateRecommendations = (await _userRecommendationService.GetByUserIdAsync(userId)).ToList()
        };
        vm.PurityGradeLabels = vm.Recommendations
            .Select(r => r.PurityGradeLabel)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Distinct().OrderBy(label => label).ToList();
        vm.PrivatePurityGradeLabels = vm.PrivateRecommendations
            .Select(r => r.PurityGradeLabel)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Distinct().OrderBy(label => label).ToList();

        SetTargetViewData(target);
        return View("~/Views/Admin/WalletManagement/Recommendations.cshtml", vm);
    }

    [HttpGet("wallet")]
    public async Task<IActionResult> Wallet(string userId)
    {
        var target = await ResolveTargetAsync(userId);
        if (target == null) return RedirectToRequests();

        return await RenderWalletAsync(target, await BuildViewModelAsync(userId, null));
    }

    [HttpGet("wallet/items/{id:int}/edit")]
    public async Task<IActionResult> EditItem(string userId, int id)
    {
        var target = await ResolveTargetAsync(userId);
        if (target == null) return RedirectToRequests();

        var wallet = await _walletService.GetByUserIdAsync(userId);
        var item = wallet?.Items.FirstOrDefault(i => i.Id == id);
        if (item == null) return NotFound();

        if (wallet!.MaxRiskPercent == null)
        {
            TempData["Error"] = "يرجى تحديد نسبة المخاطر المقبولة أولاً قبل تعديل أسهم المحفظة";
            return RedirectToAction(nameof(Wallet), new { userId });
        }

        return await RenderWalletAsync(target, await BuildViewModelAsync(userId, item));
    }

    [HttpPost("wallet/cash")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCashBalance(string userId, InvestmentWalletFormViewModel vm)
    {
        var target = await ResolveTargetAsync(userId);
        if (target == null) return RedirectToRequests();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "يرجى إدخال قيمة صالحة للسيولة";
            return RedirectToAction(nameof(Wallet), new { userId });
        }

        var newCashBalance = vm.CashBalance ?? 0;
        var wallet = await _walletService.GetByUserIdAsync(userId);
        if (wallet?.MaxRiskPercent != null)
        {
            var projectedRiskPercent = ExpectedRiskPercent(newCashBalance, wallet.Items.Sum(i => i.ExpectedLoss));
            if (projectedRiskPercent > wallet.MaxRiskPercent.Value)
            {
                TempData["Error"] = "لا يمكن تخفيض السيولة إلى هذه القيمة لأن ذلك سيجعل نسبة المخاطر المتوقعة للمحفظة تتجاوز الحد الأقصى المحدد";
                return RedirectToAction(nameof(Wallet), new { userId });
            }
        }

        try
        {
            await _walletService.CreateOrUpdateCashBalanceAsync(userId, newCashBalance);
            TempData["Success"] = "تم تحديث السيولة بنجاح";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Wallet), new { userId });
    }

    [HttpPost("wallet/risk")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetRiskPercent(string userId, WalletRiskFormViewModel vm)
    {
        var target = await ResolveTargetAsync(userId);
        if (target == null) return RedirectToRequests();

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "يرجى إدخال نسبة مخاطر صالحة بين 0 و 100";
            return RedirectToAction(nameof(Wallet), new { userId });
        }

        var wallet = await _walletService.GetByUserIdAsync(userId);
        var actualRiskPercent = ExpectedRiskPercent(wallet?.CashBalance ?? 0, wallet?.Items.Sum(i => i.ExpectedLoss) ?? 0);

        if (vm.RiskPercent!.Value < actualRiskPercent)
        {
            TempData["Error"] = "لا يمكن أن تكون نسبة المخاطر أقل من نسبة المخاطر المتوقعة الحالية للمحفظة";
            return RedirectToAction(nameof(Wallet), new { userId });
        }

        await _walletService.SetMaxRiskPercentAsync(userId, vm.RiskPercent.Value);
        TempData["Success"] = "تم تحديث نسبة المخاطر بنجاح";
        return RedirectToAction(nameof(Wallet), new { userId });
    }

    [HttpPost("wallet/items")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveItem(string userId, WalletItemFormViewModel vm)
    {
        var target = await ResolveTargetAsync(userId);
        if (target == null) return RedirectToRequests();

        var wallet = await _walletService.GetByUserIdAsync(userId);
        if (wallet == null)
        {
            TempData["Error"] = "يرجى تحديد السيولة أولاً";
            return RedirectToAction(nameof(Wallet), new { userId });
        }

        if (wallet.MaxRiskPercent == null)
        {
            TempData["Error"] = "يرجى تحديد نسبة المخاطر المقبولة أولاً قبل إضافة أسهم إلى المحفظة";
            return RedirectToAction(nameof(Wallet), new { userId });
        }

        var editingItem = vm.Id.HasValue ? wallet.Items.FirstOrDefault(i => i.Id == vm.Id.Value) : null;

        async Task<IActionResult> RerenderWithErrorsAsync()
        {
            var invalidVm = await BuildViewModelAsync(userId, editingItem);
            invalidVm.Id                  = vm.Id;
            invalidVm.StockId             = vm.StockId;
            invalidVm.Quantity            = vm.Quantity;
            invalidVm.PurchasePrice       = vm.PurchasePrice;
            invalidVm.TargetPrice         = vm.TargetPrice;
            invalidVm.StopLossPrice       = vm.StopLossPrice;
            invalidVm.PurchasePriceSource = vm.PurchasePriceSource;
            invalidVm.TargetPriceSource   = vm.TargetPriceSource;
            invalidVm.StopLossSource      = vm.StopLossSource;
            return await RenderWalletAsync(target, invalidVm);
        }

        if (!ModelState.IsValid)
            return await RerenderWithErrorsAsync();

        if (vm.Quantity is null || vm.PurchasePrice is null || vm.TargetPrice is null || vm.StopLossPrice is null || vm.StockId is null)
        {
            TempData["Error"] = "يرجى مراجعة الحقول المطلوبة";
            return RedirectToAction(nameof(Wallet), new { userId });
        }

        vm.PurchasePriceSource ??= "ClosePrice";
        vm.TargetPriceSource ??= "Resistance1";
        vm.StopLossSource ??= "Support1";

        var currentInvested = wallet.Items.Where(i => !vm.Id.HasValue || i.Id != vm.Id.Value).Sum(i => i.TotalPurchase);
        var availableCash = wallet.CashBalance - currentInvested;
        var totalPurchase = vm.Quantity.Value * vm.PurchasePrice.Value;

        if (totalPurchase > availableCash)
        {
            ModelState.AddModelError(nameof(vm.PurchasePrice), "لا يمكن أن يتجاوز اجمالي الشراء السيولة الحالية");
            return await RerenderWithErrorsAsync();
        }

        if (vm.TargetPrice.Value <= vm.PurchasePrice.Value)
        {
            ModelState.AddModelError(nameof(vm.TargetPrice), "يجب أن يكون المستهدف أعلى من سعر الشراء");
            return await RerenderWithErrorsAsync();
        }

        if (vm.StopLossPrice.Value >= vm.PurchasePrice.Value)
        {
            ModelState.AddModelError(nameof(vm.StopLossPrice), "يجب أن يكون وقف الخسارة أقل من سعر الشراء");
            return await RerenderWithErrorsAsync();
        }

        var incomingExpectedLoss = vm.Quantity.Value * (vm.PurchasePrice.Value - vm.StopLossPrice.Value);
        var otherItemsExpectedLoss = wallet.Items.Where(i => !vm.Id.HasValue || i.Id != vm.Id.Value).Sum(i => i.ExpectedLoss);
        if (ExpectedRiskPercent(wallet.CashBalance, otherItemsExpectedLoss + incomingExpectedLoss) > wallet.MaxRiskPercent!.Value)
        {
            TempData["Error"] = "لا يمكن إضافة/تعديل هذا السهم لأن ذلك سيجعل نسبة المخاطر المتوقعة للمحفظة تتجاوز الحد الأقصى المحدد";
            return RedirectToAction(nameof(Wallet), new { userId });
        }

        var targetProfitPercent = (vm.TargetPrice.Value - vm.PurchasePrice.Value) / vm.PurchasePrice.Value * 100m;
        var expectedLossPercent = (vm.PurchasePrice.Value - vm.StopLossPrice.Value) / vm.PurchasePrice.Value * 100m;
        var relativeWeightPercent = wallet.CashBalance == 0 ? 0 : totalPurchase / wallet.CashBalance * 100m;

        if (editingItem != null)
        {
            editingItem.StockId               = vm.StockId.Value;
            editingItem.Quantity              = vm.Quantity.Value;
            editingItem.PurchasePrice         = vm.PurchasePrice.Value;
            editingItem.PurchasePriceSource   = vm.PurchasePriceSource!;
            editingItem.TotalPurchase         = totalPurchase;
            editingItem.RelativeWeightPercent = relativeWeightPercent;
            editingItem.TargetPrice           = vm.TargetPrice.Value;
            editingItem.TargetPriceSource     = vm.TargetPriceSource!;
            editingItem.StopLossPrice         = vm.StopLossPrice.Value;
            editingItem.StopLossSource        = vm.StopLossSource!;
            editingItem.ExpectedProfit        = vm.Quantity.Value * (vm.TargetPrice.Value - vm.PurchasePrice.Value);
            editingItem.ExpectedLoss          = incomingExpectedLoss;
            editingItem.TargetProfitPercent   = targetProfitPercent;
            editingItem.ExpectedLossPercent   = expectedLossPercent;
            editingItem.UpdatedAt             = DateTime.UtcNow;
            await _walletService.UpdateItemAsync(editingItem);
            TempData["Success"] = "تم تحديث السهم في المحفظة";
        }
        else
        {
            await _walletService.CreateItemAsync(new WalletItem
            {
                WalletId              = wallet.Id,
                StockId               = vm.StockId.Value,
                Quantity              = vm.Quantity.Value,
                PurchasePrice         = vm.PurchasePrice.Value,
                PurchasePriceSource   = vm.PurchasePriceSource!,
                TotalPurchase         = totalPurchase,
                RelativeWeightPercent = relativeWeightPercent,
                TargetPrice           = vm.TargetPrice.Value,
                TargetPriceSource     = vm.TargetPriceSource!,
                StopLossPrice         = vm.StopLossPrice.Value,
                StopLossSource        = vm.StopLossSource!,
                ExpectedProfit        = vm.Quantity.Value * (vm.TargetPrice.Value - vm.PurchasePrice.Value),
                ExpectedLoss          = incomingExpectedLoss,
                TargetProfitPercent   = targetProfitPercent,
                ExpectedLossPercent   = expectedLossPercent
            });
            TempData["Success"] = "تمت إضافة السهم إلى المحفظة";
        }

        return RedirectToAction(nameof(Wallet), new { userId });
    }

    [HttpPost("wallet/items/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(string userId, int id)
    {
        var target = await ResolveTargetAsync(userId);
        if (target == null) return RedirectToRequests();

        var item = await _walletService.GetItemByIdAsync(id);
        if (item == null) return NotFound();

        var wallet = await _walletService.GetByUserIdAsync(userId);
        if (wallet == null || item.WalletId != wallet.Id) return Forbid();

        await _walletService.DeleteItemAsync(id);
        TempData["Success"] = "تم حذف العنصر";
        return RedirectToAction(nameof(Wallet), new { userId });
    }

    // ── المحفظة الفعلية (real wallet) ─────────────────────────────────────────
    // Mirrors RealPortfolioController, but every service call is keyed on the route's userId
    // rather than the signed-in admin. The page itself is the same shared view, repointed at
    // these routes through RealPortfolioIndexViewModel.BasePath.

    [HttpGet("portfolio")]
    public async Task<IActionResult> Portfolio(string userId)
    {
        var target = await ResolveTargetAsync(userId);
        if (target == null) return RedirectToRequests();

        return RenderPortfolio(target, await BuildPortfolioViewModelAsync(userId, null));
    }

    [HttpGet("portfolio/items/{id:int}/edit")]
    public async Task<IActionResult> EditPortfolioItem(string userId, int id)
    {
        var target = await ResolveTargetAsync(userId);
        if (target == null) return RedirectToRequests();

        var portfolio = await _portfolioService.GetByUserIdAsync(userId);
        var item = portfolio?.Items.FirstOrDefault(i => i.Id == id);
        if (item == null) return NotFound();

        return RenderPortfolio(target, await BuildPortfolioViewModelAsync(userId, item));
    }

    [HttpPost("portfolio/capital")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCapital(string userId, decimal? capital)
    {
        var target = await ResolveTargetAsync(userId);
        if (target == null) return RedirectToRequests();

        if (capital is null || capital < 0)
        {
            TempData["PortfolioError"] = "يرجى إدخال قيمة صالحة لرأس المال";
            return RedirectToAction(nameof(Portfolio), new { userId });
        }

        try
        {
            await _portfolioService.CreateOrUpdateCapitalAsync(userId, capital.Value);
            TempData["Success"] = "تم تحديث رأس المال بنجاح";
        }
        catch (InvalidOperationException ex)
        {
            TempData["PortfolioError"] = ex.Message;
        }

        return RedirectToAction(nameof(Portfolio), new { userId });
    }

    // The form posts nested under the index view model, so binding and every ModelState key
    // carry the "Form." prefix — same as RealPortfolioController.
    private const string PortfolioFormPrefix = "Form";

    [HttpPost("portfolio/items")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SavePortfolioItem(
        string userId, [Bind(Prefix = PortfolioFormPrefix)] RealPortfolioItemFormViewModel vm)
    {
        var target = await ResolveTargetAsync(userId);
        if (target == null) return RedirectToRequests();

        var portfolio = await _portfolioService.GetByUserIdAsync(userId);
        if (portfolio == null || portfolio.Capital <= 0)
        {
            TempData["PortfolioError"] = "يرجى تحديد رأس المال أولاً قبل إضافة أسهم إلى المحفظة الفعلية";
            return RedirectToAction(nameof(Portfolio), new { userId });
        }

        var editingItem = vm.Id.HasValue ? portfolio.Items.FirstOrDefault(i => i.Id == vm.Id.Value) : null;
        if (vm.Id.HasValue && editingItem == null) return NotFound();

        if (vm.PurchaseDate > DateOnly.FromDateTime(DateTime.Today))
            AddPortfolioFormError(nameof(vm.PurchaseDate), "لا يمكن أن يكون تاريخ الشراء في المستقبل");

        if (vm.StockId.HasValue && await _stockService.GetByIdAsync(vm.StockId.Value) is null)
            AddPortfolioFormError(nameof(vm.StockId), "السهم المختار غير موجود");

        var totalPurchase = (vm.Quantity ?? 0) * (vm.PurchasePrice ?? 0);
        var investedByOthers = portfolio.Items
            .Where(i => editingItem == null || i.Id != editingItem.Id)
            .Sum(i => i.TotalPurchase);
        var availableCash = portfolio.Capital - investedByOthers;

        if (ModelState.IsValid && totalPurchase > availableCash)
            AddPortfolioFormError(nameof(vm.PurchasePrice),
                $"لا يمكن أن تتجاوز القيمة الشرائية السيولة المتاحة ({availableCash:N2})");

        if (!ModelState.IsValid)
        {
            var invalidVm = await BuildPortfolioViewModelAsync(userId, editingItem);
            invalidVm.Form = await _portfolioViewModelBuilder.HydrateFormAsync(vm);
            return RenderPortfolio(target, invalidVm);
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
            TempData["Success"] = "تمت إضافة السهم إلى المحفظة الفعلية";
        }

        return RedirectToAction(nameof(Portfolio), new { userId });
    }

    [HttpPost("portfolio/items/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePortfolioItem(string userId, int id)
    {
        var target = await ResolveTargetAsync(userId);
        if (target == null) return RedirectToRequests();

        var item = await _portfolioService.GetItemByIdAsync(id);
        if (item == null) return NotFound();

        // The lot must belong to the target user's portfolio, not merely exist.
        var portfolio = await _portfolioService.GetByUserIdAsync(userId);
        if (portfolio == null || item.PortfolioId != portfolio.Id) return Forbid();

        await _portfolioService.DeleteItemAsync(id);
        TempData["Success"] = "تم حذف السهم من المحفظة الفعلية";
        return RedirectToAction(nameof(Portfolio), new { userId });
    }

    private void AddPortfolioFormError(string propertyName, string message) =>
        ModelState.AddModelError($"{PortfolioFormPrefix}.{propertyName}", message);

    private async Task<RealPortfolioIndexViewModel> BuildPortfolioViewModelAsync(string userId, RealPortfolioItem? editingItem)
    {
        var vm = await _portfolioViewModelBuilder.BuildAsync(userId, editingItem);
        vm.BasePath = $"/admin/wallet-management/{userId}/portfolio";
        vm.IsManagementMode = true;
        return vm;
    }

    private IActionResult RenderPortfolio(AppUser target, RealPortfolioIndexViewModel vm)
    {
        SetTargetViewData(target);
        return View("~/Views/RealPortfolio/Index.cshtml", vm);
    }

    private async Task<IActionResult> RenderWalletAsync(AppUser target, WalletItemFormViewModel vm)
    {
        ViewBag.Wallet = await _walletService.GetByUserIdAsync(target.Id);
        ViewBag.Summary = await _walletService.GetSummaryAsync(target.Id);
        SetTargetViewData(target);
        return View("~/Views/Admin/WalletManagement/Wallet.cshtml", vm);
    }

    /// <summary>
    /// Returns the target user only when an approved wallet-management request still exists,
    /// so revoking access takes effect immediately rather than at next login.
    /// </summary>
    private async Task<AppUser?> ResolveTargetAsync(string userId)
    {
        var target = await _userManager.FindByIdAsync(userId);
        if (target == null) return null;
        return await _managementRequestService.IsApprovedForUserAsync(userId) ? target : null;
    }

    private IActionResult RedirectToRequests()
    {
        TempData["Error"] = "لا توجد صلاحية لإدارة محفظة هذا المستخدم";
        return Redirect("/admin/wallet-management-requests");
    }

    private void SetTargetViewData(AppUser target)
    {
        ViewBag.TargetUserId = target.Id;
        ViewBag.TargetUserName = DisplayName(target);
    }

    private static string DisplayName(AppUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.FirstName) || !string.IsNullOrWhiteSpace(user.LastName))
            return $"{user.FirstName} {user.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(user.FullName)) return user.FullName;
        return user.UserName ?? "—";
    }

    private async Task<WalletItemFormViewModel> BuildViewModelAsync(string userId, WalletItem? editingItem)
    {
        var wallet = await _walletService.GetByUserIdAsync(userId);
        var generalRecs = (await _recommendationService.GetAllAsync())
            .Where(r => r.Stock != null && !r.IsHidden)
            .ToList();
        var privateRecs = (await _userRecommendationService.GetByUserIdAsync(userId))
            .Where(r => r.Stock != null)
            .ToList();

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
            vm.StockId             = editingItem.StockId;
            vm.Quantity            = editingItem.Quantity;
            vm.PurchasePrice       = editingItem.PurchasePrice;
            vm.TargetPrice         = editingItem.TargetPrice;
            vm.StopLossPrice       = editingItem.StopLossPrice;
            vm.PurchasePriceSource = editingItem.PurchasePriceSource ?? "ClosePrice";
            vm.TargetPriceSource   = editingItem.TargetPriceSource ?? "Resistance1";
            vm.StopLossSource      = editingItem.StopLossSource ?? "Support1";
        }

        return vm;
    }

    private static decimal ExpectedRiskPercent(decimal cashBalance, decimal totalExpectedLoss) =>
        cashBalance <= 0 ? 0 : totalExpectedLoss / cashBalance * 100m;
}
