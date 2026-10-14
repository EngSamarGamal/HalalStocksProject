using HalalStocks.Helpers;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HalalStocks.Controllers;

public class ArticlesController : Controller
{
    private readonly IArticleService _articleService;
    private readonly IAnalystService _analystService;
    private readonly IArticleEngagementService _engagement;
    private readonly UserManager<AppUser> _userManager;
    private const int PageSize = 12;

    public ArticlesController(IArticleService articleService, IAnalystService analystService,
        IArticleEngagementService engagement, UserManager<AppUser> userManager)
    {
        _articleService = articleService;
        _analystService = analystService;
        _engagement = engagement;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(string? search, int? category, int page = 1)
    {
        var (articles, total) = await _articleService.GetPublishedAsync(search, category, page, PageSize);
        var categories = await _articleService.GetCategoriesAsync();

        ViewData["Canonical"] = "/articles";
        if (!string.IsNullOrWhiteSpace(search) || category.HasValue || page > 1)
            ViewData["NoIndex"] = true;

        return View(new ArticleListViewModel
        {
            Articles = articles,
            // Hide auto-assigned categories (e.g. "تحليلات وآراء") from the filter — their
            // articles never appear in this listing, so the option would always yield zero.
            Categories = categories.Where(c => c.AutoAssignedForType == null)
                .Select(c => new SelectListItem(c.NameAr, c.Id.ToString(), c.Id == category)),
            Search = search,
            CategoryId = category,
            CurrentPage = page,
            TotalPages = (int)Math.Ceiling(total / (double)PageSize)
        });
    }

    [Route("articles/{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        var article = await _articleService.GetByIdAsync(id);
        if (article == null || !article.IsPublished) return NotFound();

        ViewData["Canonical"] = "/articles/" + id;
        ViewData["Description"] = PlainTextExcerpt(article.ContentAr, 160);
        if (!string.IsNullOrEmpty(article.FeaturedImagePath))
            ViewData["OgImage"] = article.FeaturedImagePath;

        var vm = new ArticleDetailViewModel { Article = article };

        var currentUser = await _userManager.GetUserAsync(User);
        vm.IsAuthenticated = currentUser != null;
        vm.CurrentUserId = currentUser?.Id;
        vm.CanModerateComments = currentUser?.Role is UserRole.SuperAdmin or UserRole.Admin or UserRole.Editor;

        // Engagement + analyst byline are analytical-only for now. The services behind them
        // are generic on Article, so enabling another type is just widening this condition.
        if (article.Type == ArticleType.Analytical)
        {
            vm.ShowEngagement = true;
            vm.AuthorName = UserDisplayNameHelper.DisplayName(article.Author);
            vm.AuthorPhotoPath = (await _analystService.GetProfileAsync(article.AuthorId))?.PhotoPath;
            vm.LikeCount = await _engagement.GetLikeCountAsync(id);
            vm.HasLiked = currentUser != null && await _engagement.HasLikedAsync(id, currentUser.Id);
            vm.Comments = await _engagement.GetCommentsAsync(id);
        }

        return View(vm);
    }

    // ── Engagement API (AJAX) ─────────────────────────────────────────────
    // All of these require authentication; a logged-out visitor sees counts and comments
    // read-only. Failures return an explicit status code *with a JSON body* rather than
    // Unauthorized()/Forbid(): a bodiless 401/403 gets rewritten to an HTML page by
    // UseStatusCodePagesWithReExecute, and Forbid() under cookie auth issues a 302 to
    // AccessDeniedPath — either of which arrives at fetch() as a 200-with-HTML, making a
    // rejected write look like a success to the client.
    private IActionResult ApiError(int status, string message) =>
        StatusCode(status, new { message });

    [HttpPost("api/articles/{id:int}/like")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleLike(int id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return ApiError(401, "الرجاء تسجيل الدخول أولاً");

        var article = await _articleService.GetByIdAsync(id);
        if (article == null || !article.IsPublished) return ApiError(404, "المقال غير موجود");

        var (isLiked, likeCount) = await _engagement.ToggleLikeAsync(id, user.Id);
        return Json(new { isLiked, likeCount });
    }

    [HttpPost("api/articles/{id:int}/comments")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int id, [FromForm] string content)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return ApiError(401, "الرجاء تسجيل الدخول أولاً");
        if (string.IsNullOrWhiteSpace(content))
            return ApiError(400, "الرجاء كتابة تعليق");

        var article = await _articleService.GetByIdAsync(id);
        if (article == null || !article.IsPublished) return ApiError(404, "المقال غير موجود");

        var comment = await _engagement.AddCommentAsync(id, user.Id, content.Trim());
        return Json(new
        {
            id = comment.Id,
            content = comment.ContentAr,
            authorName = UserDisplayNameHelper.DisplayName(comment.User),
            createdAt = comment.CreatedAt.ToString("dd/MM/yyyy"),
            isOwn = true
        });
    }

    [HttpPost("api/comments/{commentId:int}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateComment(int commentId, [FromForm] string content)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return ApiError(401, "الرجاء تسجيل الدخول أولاً");
        if (string.IsNullOrWhiteSpace(content))
            return ApiError(400, "الرجاء كتابة تعليق");

        var ok = await _engagement.UpdateCommentAsync(commentId, user.Id, content.Trim());
        if (!ok) return ApiError(403, "لا يمكنك تعديل تعليق مستخدم آخر");
        return Json(new { content = content.Trim() });
    }

    [HttpPost("api/comments/{commentId:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return ApiError(401, "الرجاء تسجيل الدخول أولاً");

        // An admin may remove any comment; everyone else only their own.
        var isModerator = user.Role is UserRole.SuperAdmin or UserRole.Admin or UserRole.Editor;
        var ok = isModerator
            ? await _engagement.DeleteCommentAsAdminAsync(commentId)
            : await _engagement.DeleteCommentAsync(commentId, user.Id);

        if (!ok) return ApiError(403, "لا يمكنك حذف تعليق مستخدم آخر");
        return Json(new { deleted = true });
    }

    // Strips HTML tags from Quill-authored content for use as a meta description.
    private static string PlainTextExcerpt(string html, int maxLength)
    {
        var text = System.Text.RegularExpressions.Regex.Replace(html ?? "", "<.*?>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        return text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "…";
    }
}
