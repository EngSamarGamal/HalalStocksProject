using System.Security.Claims;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalalStocks.Controllers;

[Route("fatawa")]
public class FatawaController : Controller
{
    private readonly IFatwaService _fatwaService;
    private const int PageSize = 5;
    // Keeps the admin queue from being flooded by a single account.
    private const int MaxPendingPerUser = 5;

    public FatawaController(IFatwaService fatwaService) => _fatwaService = fatwaService;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        if (!string.IsNullOrWhiteSpace(search) || page > 1)
            ViewData["NoIndex"] = true;
        ViewData["Canonical"] = "/fatawa";

        return View(await BuildIndexAsync(search, page, new FatwaAskViewModel()));
    }

    // "عرض المزيد" — returns just the next page of cards; X-Has-More tells the client whether to keep the button.
    [HttpGet("more")]
    public async Task<IActionResult> More(string? search, int page = 2)
    {
        var (items, total) = await _fatwaService.GetPublishedAsync(search, page, PageSize);
        Response.Headers["X-Has-More"] = (page * PageSize < total) ? "1" : "0";
        return PartialView("~/Views/Fatawa/_FatwaList.cshtml", items);
    }

    [Authorize]
    [HttpPost("ask")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask([Bind(Prefix = "Ask")] FatwaAskViewModel ask)
    {
        if (!ModelState.IsValid)
            return View("Index", await BuildIndexAsync(null, 1, ask));

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (await _fatwaService.CountPendingForUserAsync(userId) >= MaxPendingPerUser)
        {
            ModelState.AddModelError("Ask.Question", "لديك عدة أسئلة بانتظار الرد بالفعل، يرجى الانتظار حتى تتم الإجابة عليها.");
            return View("Index", await BuildIndexAsync(null, 1, ask));
        }

        await _fatwaService.AskAsync(userId, ask.Question);
        TempData["FatwaSuccess"] = "تم استلام سؤالك بنجاح. ستتم مراجعته والإجابة عليه، وسيظهر هنا بعد نشر الإجابة.";
        return Redirect("/fatawa#ask");
    }

    private async Task<FatwaIndexViewModel> BuildIndexAsync(string? search, int page, FatwaAskViewModel ask)
    {
        var (items, total) = await _fatwaService.GetPublishedAsync(search, page, PageSize);
        return new FatwaIndexViewModel
        {
            Questions = items,
            Search = search,
            CurrentPage = Math.Max(page, 1),
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)PageSize),
            IsAuthenticated = User.Identity?.IsAuthenticated == true,
            Ask = ask
        };
    }
}
