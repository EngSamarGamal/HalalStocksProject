using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Controllers;

[Route("portfolio-request")]
public class PortfolioRequestController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;

    public PortfolioRequestController(AppDbContext db, UserManager<AppUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var sections = await _db.PortfolioInfoSections
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();

        ViewBag.Sections = sections;

        if (!User.Identity!.IsAuthenticated)
            return View();

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return View();

        var activeRequest = await GetActiveRequestAsync(user.Id);
        if (activeRequest != null)
        {
            ViewBag.HasActiveRequest = true;
            return View();
        }

        var vm = new PortfolioRequestFormViewModel
        {
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            ContactEmail = user.Email ?? string.Empty
        };

        return View(vm);
    }

    [HttpGet("index")]
    public IActionResult IndexLegacy() => RedirectPermanent("/portfolio-request");

    [HttpPost("")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(PortfolioRequestFormViewModel vm)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var activeRequest = await GetActiveRequestAsync(user.Id);
        if (activeRequest != null)
        {
            TempData["Error"] = "لديك طلب قيد المراجعة بالفعل.";
            return RedirectToAction(nameof(MyRequest));
        }

        if (vm.Residence == ResidenceType.OutsideEgypt &&
            string.IsNullOrWhiteSpace(vm.CountryOfResidence))
            ModelState.AddModelError(nameof(vm.CountryOfResidence), "يرجى تحديد الدولة");

        if (vm.PreferredContact == ContactMethod.WhatsApp &&
            string.IsNullOrWhiteSpace(vm.WhatsAppNumber))
            ModelState.AddModelError(nameof(vm.WhatsAppNumber), "رقم واتس آب مطلوب");

        if (vm.PreferredContact == ContactMethod.Email &&
            string.IsNullOrWhiteSpace(vm.ContactEmail))
            ModelState.AddModelError(nameof(vm.ContactEmail), "البريد الإلكتروني مطلوب");

        if (!ModelState.IsValid)
        {
            ViewBag.Sections = await _db.PortfolioInfoSections
                .Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ToListAsync();
            return View("Index", vm);
        }

        var request = new PortfolioRequest
        {
            UserId           = user.Id,
            PhoneNumber      = vm.PhoneNumber,
            Governorate      = vm.Governorate,
            Residence        = vm.Residence,
            CountryOfResidence = vm.Residence == ResidenceType.OutsideEgypt ? vm.CountryOfResidence : null,
            InvestmentCapital = vm.InvestmentCapital,
            Horizon          = vm.Horizon,
            StockType        = vm.StockType,
            PreferredContact = vm.PreferredContact,
            WhatsAppNumber   = vm.PreferredContact == ContactMethod.WhatsApp ? vm.WhatsAppNumber : null,
            ContactEmail     = vm.PreferredContact == ContactMethod.Email    ? vm.ContactEmail   : null,
            ContractDelivery = vm.ContractDelivery,
            Status           = PortfolioRequestStatus.New
        };

        _db.PortfolioRequests.Add(request);
        await _db.SaveChangesAsync();

        TempData["Success"] = "تم إرسال طلبك بنجاح، سيتم التواصل معك قريباً إن شاء الله";
        return RedirectToAction(nameof(MyRequest));
    }

    [Route("my-request")]
    [Authorize]
    public async Task<IActionResult> MyRequest()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var request = await _db.PortfolioRequests
            .Where(r => r.UserId == user.Id)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (request == null)
            return RedirectToAction(nameof(Index));

        return View(request);
    }

    private async Task<PortfolioRequest?> GetActiveRequestAsync(string userId)
    {
        var activeStatuses = new[]
        {
            PortfolioRequestStatus.New,
            PortfolioRequestStatus.UnderReview,
            PortfolioRequestStatus.Contacted,
            PortfolioRequestStatus.ActiveClient
        };

        return await _db.PortfolioRequests
            .Where(r => r.UserId == userId && activeStatuses.Contains(r.Status))
            .FirstOrDefaultAsync();
    }
}
