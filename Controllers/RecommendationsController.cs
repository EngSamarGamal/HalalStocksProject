using HalalStocks.Helpers;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HalalStocks.Controllers;

[Authorize]
[Route("recommendations")]
public class RecommendationsController : Controller
{
    private readonly IRecommendationService        _recommendationService;
    private readonly IUserRecommendationService    _userRecommendationService;
    private readonly IRecommendationRequestService _requestService;
    private readonly IWalletManagementRequestService _managementRequestService;
    private readonly IStockService                 _stockService;
    private readonly UserManager<AppUser>          _userManager;

    public RecommendationsController(
        IRecommendationService recommendationService,
        IUserRecommendationService userRecommendationService,
        IRecommendationRequestService requestService,
        IWalletManagementRequestService managementRequestService,
        IStockService stockService,
        UserManager<AppUser> userManager)
    {
        _recommendationService = recommendationService;
        _userRecommendationService = userRecommendationService;
        _requestService        = requestService;
        _managementRequestService = managementRequestService;
        _stockService          = stockService;
        _userManager           = userManager;
    }

    [AllowAnonymous]
    [Route("")]
    public async Task<IActionResult> Index()
    {
        var user    = await _userManager.GetUserAsync(User);
        if (user == null) return View("~/Views/Recommendations/GuestGate.cshtml");

        var isAdmin = IsAdminUser(user);
        var request = await _requestService.GetByUserIdAsync(user.Id);

        var vm = new RecommendationsIndexViewModel
        {
            IsAdmin     = isAdmin,
            UserRequest = request,
            PurityGradeLabels = (await _recommendationService.GetDistinctPurityGradeLabelsAsync()).ToList()
        };

        if (isAdmin || request?.Status == RecommendationRequestStatus.Approved)
        {
            vm.PageState        = RecommendationsPageState.List;
            vm.Recommendations  = (await _recommendationService.GetAllAsync()).ToList();
            if (!isAdmin)
                vm.Recommendations = vm.Recommendations.Where(r => !r.IsHidden).ToList();
            vm.PrivateRecommendations = (await _userRecommendationService.GetByUserIdAsync(user.Id)).ToList();
            vm.PrivatePurityGradeLabels = vm.PrivateRecommendations
                .Select(r => r.PurityGradeLabel)
                .Where(label => !string.IsNullOrWhiteSpace(label))
                .Distinct()
                .OrderBy(label => label)
                .ToList();

            // The wallet-management request covers all three tabs, so its banner renders here too.
            ViewBag.ManagementStatus = (await _managementRequestService.GetByUserIdAsync(user.Id))?.Status;
        }
        else if (request?.Status == RecommendationRequestStatus.Pending)
        {
            vm.PageState = RecommendationsPageState.Pending;
        }
        else
        {
            vm.PageState = RecommendationsPageState.Gate;
        }

        return View("~/Views/Recommendations/Index.cshtml", vm);
    }

    [HttpPost, Route("join-request")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> JoinRequest()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (IsAdminUser(user))
            return RedirectToAction(nameof(Index));

        var request = await _requestService.GetByUserIdAsync(user.Id);
        if (request?.Status == RecommendationRequestStatus.Pending)
            return RedirectToAction(nameof(Index));

        await _requestService.UpsertPendingRequestAsync(user.Id);
        TempData["Success"] = "تم ارسال الطلب.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "SuperAdmin,Admin,Editor")]
    [Route("create")]
    public async Task<IActionResult> Create()
    {
        var vm = new StockRecommendationFormViewModel
        {
            RecommendationTypes = (await _recommendationService.GetRecommendationTypesAsync())
        };
        return View("~/Views/Recommendations/Create.cshtml", vm);
    }

    [Authorize(Roles = "SuperAdmin,Admin,Editor")]
    [HttpPost, Route("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StockRecommendationFormViewModel vm)
    {
        if (string.IsNullOrWhiteSpace(vm.GeneralTrend))
            ModelState.AddModelError(nameof(vm.GeneralTrend), "يرجى إدخال الاتجاه العام");

        if (!ModelState.IsValid)
        {
            vm.RecommendationTypes = await _recommendationService.GetRecommendationTypesAsync();
            return View("~/Views/Recommendations/Create.cshtml", vm);
        }

        if (await _recommendationService.ExistsForStockAsync(vm.StockId))
        {
            ModelState.AddModelError(nameof(vm.StockId), "هذا السهم لديه توصية بالفعل");
            return View("~/Views/Recommendations/Create.cshtml", vm);
        }

        await _recommendationService.CreateAsync(MapToEntity(vm));
        TempData["Success"] = "تمت إضافة التوصية بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "SuperAdmin,Admin,Editor")]
    [Route("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var recommendation = await _recommendationService.GetByIdAsync(id);
        if (recommendation == null) return NotFound();

        var vm = MapToViewModel(recommendation);
        vm.RecommendationTypes = await _recommendationService.GetRecommendationTypesAsync();
        return View("~/Views/Recommendations/Edit.cshtml", vm);
    }

    [Authorize(Roles = "SuperAdmin,Admin,Editor")]
    [HttpPost, Route("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StockRecommendationFormViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        if (string.IsNullOrWhiteSpace(vm.GeneralTrend))
            ModelState.AddModelError(nameof(vm.GeneralTrend), "يرجى إدخال الاتجاه العام");

        if (!ModelState.IsValid)
        {
            vm.RecommendationTypes = await _recommendationService.GetRecommendationTypesAsync();
            return View("~/Views/Recommendations/Edit.cshtml", vm);
        }

        if (await _recommendationService.ExistsForStockAsync(vm.StockId, id))
        {
            ModelState.AddModelError(nameof(vm.StockId), "هذا السهم لديه توصية بالفعل");
            return View("~/Views/Recommendations/Edit.cshtml", vm);
        }

        var updated = await _recommendationService.UpdateAsync(MapToEntity(vm));
        if (updated == null) return NotFound();

        TempData["Success"] = "تم تحديث التوصية بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "SuperAdmin,Admin,Editor")]
    [HttpPost, Route("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await _recommendationService.DeleteAsync(id))
            return NotFound();

        TempData["Success"] = "تم حذف التوصية";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "SuperAdmin,Admin,Editor")]
    [Route("types")]
    public async Task<IActionResult> Types()
    {
        var types = await _recommendationService.GetRecommendationTypesAsync();
        return View("~/Views/Recommendations/Types.cshtml", types);
    }

    [Authorize(Roles = "SuperAdmin,Admin,Editor")]
    [HttpPost, Route("types")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Types(string nameAr, bool isExpiredMarker)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
        {
            TempData["Error"] = "يرجى إدخال اسم النوع";
            return RedirectToAction(nameof(Types));
        }

        await _recommendationService.CreateRecommendationTypeAsync(new RecommendationType
        {
            NameAr = nameAr.Trim(),
            IsExpiredMarker = isExpiredMarker
        });

        TempData["Success"] = "تمت إضافة نوع التوصية";
        return RedirectToAction(nameof(Types));
    }

    [Authorize(Roles = "SuperAdmin,Admin,Editor")]
    [HttpPost, Route("types/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteType(int id)
    {
        if (await _recommendationService.RecommendationTypeInUseAsync(id))
        {
            TempData["Error"] = "لا يمكن حذف هذا النوع لوجود توصيات مرتبطة به";
            return RedirectToAction(nameof(Types));
        }

        if (!await _recommendationService.DeleteRecommendationTypeAsync(id))
            return NotFound();

        TempData["Success"] = "تم حذف نوع التوصية";
        return RedirectToAction(nameof(Types));
    }

    [Route("my/create")]
    public async Task<IActionResult> MyCreate()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        if (!await IsApprovedAsync(user)) return RedirectToAction(nameof(Index));

        var vm = new StockRecommendationFormViewModel
        {
            RecommendationTypes = (await _recommendationService.GetRecommendationTypesAsync())
        };
        return View("~/Views/Recommendations/MyCreate.cshtml", vm);
    }

    [HttpPost, Route("my/create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MyCreate(StockRecommendationFormViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        if (!await IsApprovedAsync(user)) return RedirectToAction(nameof(Index));

        // Private recommendations don't collect this field (see _FormFields.cshtml).
        ModelState.Remove(nameof(vm.YtdChangePercent));

        if (!ModelState.IsValid)
        {
            vm.RecommendationTypes = await _recommendationService.GetRecommendationTypesAsync();
            return View("~/Views/Recommendations/MyCreate.cshtml", vm);
        }

        if (await _userRecommendationService.ExistsForUserStockAsync(user.Id, vm.StockId))
        {
            ModelState.AddModelError(nameof(vm.StockId), "هذا السهم موجود بالفعل ضمن توصياتك الخاصة");
            vm.RecommendationTypes = await _recommendationService.GetRecommendationTypesAsync();
            return View("~/Views/Recommendations/MyCreate.cshtml", vm);
        }

        await _userRecommendationService.CreateAsync(MapToUserEntity(vm, user.Id));
        TempData["Success"] = "تمت إضافة التوصية الخاصة بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [Route("my/{id:int}/edit")]
    public async Task<IActionResult> MyEdit(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        if (!await IsApprovedAsync(user)) return RedirectToAction(nameof(Index));

        var recommendation = await _userRecommendationService.GetOwnedByIdAsync(id, user.Id);
        if (recommendation == null) return NotFound();

        var vm = MapToViewModel(recommendation);
        vm.RecommendationTypes = await _recommendationService.GetRecommendationTypesAsync();
        return View("~/Views/Recommendations/MyEdit.cshtml", vm);
    }

    [HttpPost, Route("my/{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MyEdit(int id, StockRecommendationFormViewModel vm)
    {
        if (id != vm.Id) return BadRequest();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();
        if (!await IsApprovedAsync(user)) return RedirectToAction(nameof(Index));

        // Private recommendations don't collect this field (see _FormFields.cshtml).
        ModelState.Remove(nameof(vm.YtdChangePercent));

        if (!ModelState.IsValid)
        {
            vm.RecommendationTypes = await _recommendationService.GetRecommendationTypesAsync();
            return View("~/Views/Recommendations/MyEdit.cshtml", vm);
        }

        if (await _userRecommendationService.ExistsForUserStockAsync(user.Id, vm.StockId, id))
        {
            ModelState.AddModelError(nameof(vm.StockId), "هذا السهم موجود بالفعل ضمن توصياتك الخاصة");
            vm.RecommendationTypes = await _recommendationService.GetRecommendationTypesAsync();
            return View("~/Views/Recommendations/MyEdit.cshtml", vm);
        }

        var entity = MapToUserEntity(vm, user.Id);
        entity.Id = id;
        var updated = await _userRecommendationService.UpdateAsync(entity, user.Id);
        if (updated == null) return NotFound();

        TempData["Success"] = "تم تحديث التوصية الخاصة بنجاح";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Route("my/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MyDelete(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (!await _userRecommendationService.DeleteAsync(id, user.Id))
            return NotFound();

        TempData["Success"] = "تم حذف التوصية الخاصة";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/api/stocks/active")]
    public async Task<IActionResult> ActiveStocks()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        var stocks = await _stockService.GetAllActiveStocksAsync();
        return Json(stocks.Select(s => new
        {
            id               = s.Id,
            ticker           = s.TickerSymbol,
            nameAr           = s.NameAr,
            purityGradeLabel = PurityGradeHelper.Format(s.PurityGrades)
        }));
    }

    private static bool IsAdminUser(AppUser user) =>
        user.Role is UserRole.SuperAdmin or UserRole.Admin or UserRole.Editor;

    private async Task<bool> IsApprovedAsync(AppUser user)
    {
        if (IsAdminUser(user)) return true;
        var request = await _requestService.GetByUserIdAsync(user.Id);
        return request?.Status == RecommendationRequestStatus.Approved;
    }

    private static StockRecommendation MapToEntity(StockRecommendationFormViewModel vm) => new()
    {
        Id                = vm.Id ?? 0,
        StockId           = vm.StockId,
        PurityGradeLabel  = vm.PurityGradeLabel.Trim(),
        GeneralTrend      = (vm.GeneralTrend ?? string.Empty).Trim(),
        ClosingPrice      = vm.ClosingPrice ?? 0,
        Support1          = vm.Support1 ?? 0,
        Support2          = vm.Support2 ?? 0,
        Resistance1       = vm.Resistance1 ?? 0,
        Resistance2       = vm.Resistance2 ?? 0,
        StopLoss          = vm.StopLoss ?? 0,
        YtdChangePercent  = vm.YtdChangePercent ?? 0,
        RecommendationTypeId = vm.RecommendationTypeId,
        Notes             = string.IsNullOrWhiteSpace(vm.Notes) ? null : vm.Notes.Trim(),
        IsHidden          = vm.IsHidden
    };

    private static StockRecommendationFormViewModel MapToViewModel(RecommendationBase r) => new()
    {
        Id               = r.Id,
        StockId          = r.StockId,
        PurityGradeLabel = r.PurityGradeLabel,
        GeneralTrend     = r.GeneralTrend,
        ClosingPrice     = r.ClosingPrice,
        Support1         = r.Support1,
        Support2         = r.Support2,
        Resistance1      = r.Resistance1,
        Resistance2      = r.Resistance2,
        StopLoss         = r.StopLoss,
        YtdChangePercent = r.YtdChangePercent,
        RecommendationTypeId = r.RecommendationTypeId,
        RecommendationTypes = new List<RecommendationType>(),
        Notes            = r.Notes,
        IsHidden         = (r as StockRecommendation)?.IsHidden ?? false
    };

    private static UserStockRecommendation MapToUserEntity(StockRecommendationFormViewModel vm, string userId) => new()
    {
        UserId            = userId,
        StockId           = vm.StockId,
        PurityGradeLabel  = vm.PurityGradeLabel.Trim(),
        GeneralTrend      = (vm.GeneralTrend ?? string.Empty).Trim(),
        ClosingPrice      = vm.ClosingPrice ?? 0,
        Support1          = vm.Support1 ?? 0,
        Support2          = vm.Support2 ?? 0,
        Resistance1       = vm.Resistance1 ?? 0,
        Resistance2       = vm.Resistance2 ?? 0,
        StopLoss          = vm.StopLoss ?? 0,
        YtdChangePercent  = vm.YtdChangePercent ?? 0,
        RecommendationTypeId = vm.RecommendationTypeId,
        Notes             = string.IsNullOrWhiteSpace(vm.Notes) ? null : vm.Notes.Trim()
    };
}
