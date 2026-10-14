using System.Text;
using System.Xml.Linq;
using HalalStocks.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HalalStocks.Controllers;

public class SeoController : Controller
{
    private readonly IArticleService _articleService;
    private readonly IStockService   _stockService;
    private readonly IConfiguration  _cfg;

    public SeoController(IArticleService articleService, IStockService stockService, IConfiguration cfg)
    {
        _articleService = articleService;
        _stockService   = stockService;
        _cfg            = cfg;
    }

    [Route("sitemap.xml")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Sitemap()
    {
        var baseUrl = (_cfg["SiteUrl"] ?? "https://kasheif.com").TrimEnd('/');
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        XElement Url(string path, DateTime? lastMod = null, string changeFreq = "weekly", string priority = "0.5")
        {
            var el = new XElement(ns + "url",
                new XElement(ns + "loc", baseUrl + path),
                new XElement(ns + "changefreq", changeFreq),
                new XElement(ns + "priority", priority));
            if (lastMod.HasValue)
                el.Add(new XElement(ns + "lastmod", lastMod.Value.ToString("yyyy-MM-dd")));
            return el;
        }

        var urls = new List<XElement>
        {
            Url("/", changeFreq: "daily", priority: "1.0"),
            Url("/articles", changeFreq: "daily", priority: "0.8"),
            Url("/analytics", changeFreq: "daily", priority: "0.8"),
            Url("/videos", priority: "0.6"),
            Url("/fatawa", changeFreq: "daily", priority: "0.7"),
            Url("/calculator", priority: "0.7"),
            Url("/zakat", priority: "0.7"),
            Url("/portfolio-request", priority: "0.7")
        };

        var (articles, _) = await _articleService.GetPublishedAsync(null, null, 1, 10000);
        urls.AddRange(articles.Select(a => Url($"/articles/{a.Id}", a.UpdatedAt, priority: "0.6")));

        // Analytical articles live outside GetPublishedAsync (they're excluded from
        // /articles), so they and their analysts' public pages are added separately.
        var (analytical, _) = await _articleService.GetPublicAnalyticalAsync(null, 1, 10000);
        urls.AddRange(analytical.Select(a => Url($"/articles/{a.Id}", a.UpdatedAt, priority: "0.6")));
        urls.AddRange(analytical.Select(a => a.AuthorId).Distinct()
            .Select(authorId => Url($"/analysts/{authorId}", priority: "0.5")));

        // A few TickerSymbol values carry stray leading/trailing whitespace from data entry;
        // trim so the sitemap never publishes a URL containing a literal space.
        var stocks = await _stockService.GetAllActiveStocksAsync();
        urls.AddRange(stocks.Select(s => Url($"/stocks/{s.TickerSymbol.Trim()}", priority: "0.5")));

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(ns + "urlset", urls));

        return Content(doc.Declaration + Environment.NewLine + doc.ToString(), "application/xml", Encoding.UTF8);
    }
}
