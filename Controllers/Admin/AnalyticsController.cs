using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalalStocks.Controllers.Admin;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
[Route("admin/analytics")]
public class AnalyticsController : Controller
{
    private readonly IArticleService _articleService;
    private readonly IAnalystService _analystService;
    private readonly IUserService _userService;
    private const int PageSize = 20;

    public AnalyticsController(IArticleService articleService, IAnalystService analystService, IUserService userService)
    {
        _articleService = articleService;
        _analystService = analystService;
        _userService = userService;
    }

    [Route("")]
    public async Task<IActionResult> Index(string tab = "analysts", int page = 1)
    {
        var vm = new AnalyticsIndexViewModel
        {
            Tab = tab,
            CurrentPage = page,
            AnalystsCount = (await _analystService.GetAllProfilesAsync()).Count(),
            ArticlesCount = await _articleService.GetAnalyticalArticleCountAsync(),
            PendingCount = await _articleService.GetPendingArticleCountAsync()
        };

        if (tab == "pending")
        {
            var (items, total) = await _articleService.GetPendingArticlesAsync(page, PageSize);
            vm.Articles = items;
            vm.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        }
        else if (tab == "articles")
        {
            var (items, total) = await _articleService.GetAnalyticalArticlesAsync(page, PageSize);
            vm.Articles = items;
            vm.TotalPages = (int)Math.Ceiling(total / (double)PageSize);
        }
        else
        {
            var profiles = await _analystService.GetAllProfilesAsync();
            var summaries = new List<AnalystSummary>();
            foreach (var profile in profiles)
            {
                var (total, pending) = await _analystService.GetArticleCountsAsync(profile.UserId);
                summaries.Add(new AnalystSummary { Profile = profile, TotalArticles = total, PendingArticles = pending });
            }
            vm.Analysts = summaries;
        }

        return View("~/Views/Admin/Analytics/Index.cshtml", vm);
    }

    [Route("analysts/{userId}")]
    public async Task<IActionResult> AnalystDetail(string userId, int page = 1)
    {
        var profile = await _analystService.GetProfileAsync(userId);
        if (profile == null) return NotFound();

        var (items, total) = await _analystService.GetOwnArticlesAsync(userId, page, PageSize);
        return View("~/Views/Admin/Analytics/AnalystDetail.cshtml", new AnalystDetailViewModel
        {
            Profile = profile,
            Articles = items,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(total / (double)PageSize)
        });
    }

    [HttpPost, Route("analysts/{userId}/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAnalyst(string userId)
    {
        var (success, errors) = await _userService.RemoveAnalystDesignationAsync(userId);
        TempData[success ? "Success" : "Error"] = success
            ? "تمت إزالة صفة المحلل. مقالاته السابقة تبقى كما هي ويمكن إدارتها من هنا."
            : string.Join(" | ", errors);
        return Redirect("/admin/analytics?tab=analysts");
    }

    [Route("articles/{id:int}")]
    public async Task<IActionResult> ArticleDetail(int id)
    {
        var article = await _articleService.GetByIdAsync(id);
        if (article == null || article.Type != ArticleType.Analytical) return NotFound();
        return View("~/Views/Admin/Analytics/ArticleDetail.cshtml", article);
    }

    [HttpPost, Route("articles/{id:int}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        await _articleService.ApproveArticleAsync(id);
        TempData["Success"] = "تمت الموافقة على المقال ونُشر";
        return Redirect("/admin/analytics?tab=pending");
    }

    [HttpPost, Route("articles/{id:int}/hide")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Hide(int id, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["Error"] = "الرجاء إدخال سبب الإخفاء";
            return Redirect($"/admin/analytics/articles/{id}");
        }

        await _articleService.HideArticleAsync(id, reason.Trim());
        TempData["Success"] = "تم إخفاء المقال";
        return Redirect("/admin/analytics?tab=articles");
    }

    [HttpPost, Route("articles/{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _articleService.DeleteAsync(id);
        TempData["Success"] = "تم حذف المقال";
        return Redirect("/admin/analytics?tab=articles");
    }
}
