using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HalalStocks.Controllers;

public class HomeController : Controller
{
    private readonly IStockService _stockService;
    private readonly IArticleService _articleService;
    private readonly IVideoService _videoService;
    private readonly IAnalystService _analystService;
    private readonly IBannerService _bannerService;
    private readonly ICalculatorInstructionService _instructionService;
    private readonly IFatwaService _fatwaService;
    private const int PageSize = 20;

    public HomeController(IStockService stockService, IArticleService articleService,
        IVideoService videoService, IAnalystService analystService, IBannerService bannerService,
        ICalculatorInstructionService instructionService, IFatwaService fatwaService)
    {
        _stockService = stockService;
        _articleService = articleService;
        _videoService = videoService;
        _analystService = analystService;
        _bannerService = bannerService;
        _instructionService = instructionService;
        _fatwaService = fatwaService;
    }

    public async Task<IActionResult> Index(string? search, List<int>? categoryIds, List<int>? indexIds, ShariahStatus? shariahStatus, int page = 1, bool pure = false, bool semiPure = false)
    {
        var searchSubmitted = !string.IsNullOrWhiteSpace(search) || (categoryIds != null && categoryIds.Count > 0) || (indexIds != null && indexIds.Count > 0) || shariahStatus.HasValue || pure || semiPure || page > 1;

        ViewData["Canonical"] = "/";
        if (searchSubmitted)
            ViewData["NoIndex"] = true;

        IEnumerable<Stock> stocks = new List<Stock>();
        int total = 0;
        if (searchSubmitted)
        {
            (stocks, total) = await _stockService.GetStocksAsync(search, categoryIds, shariahStatus, indexIds, page, PageSize, filterPure: pure, filterSemiPure: semiPure);
        }

        var categories = await _stockService.GetCategoriesAsync();
        var indices = await _stockService.GetIndicesAsync();
        var latestArticles = await _articleService.GetLatestAsync(4);
        var latestVideos = await _videoService.GetLatestAsync(6);
        var banners = await _bannerService.GetActiveOrderedAsync();
        // First 5 show; the rest are revealed by the "عرض المزيد" button. Capped at 20, the rest live on /fatawa.
        var featuredFatawa = await _fatwaService.GetFeaturedAsync(20);
        var homeAlertsHtml = await _instructionService.GetContentAsync(CalculatorType.HomeAlerts);
        // Cap assumed pending client confirmation — no "view all" page exists for this section yet;
        // this constant is where pagination would hook in if one is added later.
        var shariahStandards = await _articleService.GetShariahStandardsAsync(20);

        // Latest 10 analytical articles per the client's spec; anything beyond that is
        // reachable via the "عرض كل التحليلات" button -> /analytics.
        var analyticalArticles = await _articleService.GetLatestAnalyticalAsync(10);
        var analystProfiles = await _analystService.GetProfilesByUserIdsAsync(analyticalArticles.Select(a => a.AuthorId));
        var latestAnalytical = AnalyticalArticleCardViewModel.FromMany(analyticalArticles, analystProfiles);

        var vm = new HomeViewModel
        {
            Stocks = stocks,
            Categories = categories,
            Indices = indices,
            LatestArticles = latestArticles,
            ShariahStandards = shariahStandards,
            LatestAnalytical = latestAnalytical,
            LatestVideos = latestVideos,
            Banners = banners,
            HomeAlertsHtml = homeAlertsHtml,
            FeaturedFatawa = featuredFatawa,
            Search = search,
            CategoryIds = categoryIds ?? new List<int>(),
            IndexIds = indexIds ?? new List<int>(),
            ShariahStatus = shariahStatus,
            Pure = pure,
            SemiPure = semiPure,
            CurrentPage = page,
            TotalCount = total,
            TotalPages = (int)Math.Ceiling(total / (double)PageSize),
            SearchSubmitted = searchSubmitted
        };
        return View(vm);
    }

    [Route("error/{code:int}")]
    public IActionResult Error(int code)
    {
        ViewBag.Code = code;
        return View("~/Views/Shared/Error.cshtml");
    }
}
