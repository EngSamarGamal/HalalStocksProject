using HalalStocks.Helpers;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using HalalStocks.Models;

namespace HalalStocks.Controllers;

public class AnalyticsController : Controller
{
    private readonly IArticleService _articleService;
    private readonly IAnalystService _analystService;
    private readonly UserManager<AppUser> _userManager;
    private const int PageSize = 12;

    public AnalyticsController(IArticleService articleService, IAnalystService analystService,
        UserManager<AppUser> userManager)
    {
        _articleService = articleService;
        _analystService = analystService;
        _userManager = userManager;
    }

    [Route("analytics")]
    public async Task<IActionResult> Index(string? search, int page = 1)
    {
        var (articles, total) = await _articleService.GetPublicAnalyticalAsync(search, page, PageSize);
        var profiles = await _analystService.GetProfilesByUserIdsAsync(articles.Select(a => a.AuthorId));

        ViewData["Canonical"] = "/analytics";
        if (!string.IsNullOrWhiteSpace(search) || page > 1)
            ViewData["NoIndex"] = true;

        return View("~/Views/Analytics/Index.cshtml", new AnalyticsListViewModel
        {
            Articles = AnalyticalArticleCardViewModel.FromMany(articles, profiles),
            Search = search,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(total / (double)PageSize)
        });
    }

    [Route("analysts/{userId}")]
    public async Task<IActionResult> AnalystProfile(string userId, string? search, int page = 1)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        // The profile row is optional (an admin can author analytical articles without
        // ever being designated an analyst), so this page is driven by the AppUser and
        // simply renders without bio/photo when no AnalystProfile exists.
        var profile = await _analystService.GetProfileAsync(userId);

        var (articles, total) = await _articleService.GetPublicAnalyticalByAuthorAsync(userId, search, page, PageSize);

        // Don't expose a bare user page for someone with no public analytical output.
        if (!articles.Any() && profile == null) return NotFound();

        var profiles = await _analystService.GetProfilesByUserIdsAsync(articles.Select(a => a.AuthorId));

        ViewData["Canonical"] = "/analysts/" + userId;
        if (!string.IsNullOrWhiteSpace(search) || page > 1)
            ViewData["NoIndex"] = true;

        return View("~/Views/Analytics/AnalystProfile.cshtml", new AnalystPublicProfileViewModel
        {
            UserId = userId,
            Name = UserDisplayNameHelper.DisplayName(user),
            PhotoPath = profile?.PhotoPath,
            SummaryAr = profile?.SummaryAr,
            Articles = AnalyticalArticleCardViewModel.FromMany(articles, profiles),
            Search = search,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(total / (double)PageSize)
        });
    }
}
