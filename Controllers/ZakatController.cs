using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HalalStocks.Controllers;

[Route("zakat")]
public class ZakatController : Controller
{
    private readonly IZakatService         _zakat;
    private readonly IGoldPriceService     _gold;
    private readonly ILogger<ZakatController> _logger;

    public ZakatController(IZakatService zakat, IGoldPriceService gold,
                           ILogger<ZakatController> logger)
    {
        _zakat  = zakat;
        _gold   = gold;
        _logger = logger;
    }

    private bool IsAuth => User.Identity?.IsAuthenticated == true;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET /zakat  (optional ?edit=id to pre-load edit mode)
    [HttpGet("")]
    public IActionResult Index([FromQuery] int? edit = null)
    {
        if (!IsAuth) return View("~/Views/Zakat/GuestGate.cshtml");
        ViewBag.EditId = edit;
        return View("~/Views/Zakat/Index.cshtml");
    }

    // GET /zakat/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        if (!IsAuth) return Redirect($"/account/login?returnUrl=/zakat/{id}");
        var record = await _zakat.GetRecordAsync(id, UserId);
        if (record == null) return NotFound();
        return View("~/Views/Zakat/Detail.cshtml", record);
    }

    // GET /zakat/gold-price
    [HttpGet("gold-price")]
    public async Task<IActionResult> GoldPrice()
    {
        if (!IsAuth) return Unauthorized();

        var price = await _gold.GetGold21KGramPriceInEGPAsync();

        if (price < 0)
        {
            _logger.LogWarning("ZakatController: gold price fetch failed (API error or missing key)");
            return Ok(new { success = false, message = "تعذر جلب سعر الذهب تلقائياً، يرجى المحاولة لاحقاً" });
        }

        var snapshot  = await _gold.GetSnapshotAsync();
        var nisab     = Math.Round(price * 85, 2);
        var fetchedAt = snapshot?.FetchedAt.ToLocalTime().ToString("yyyy/MM/dd HH:mm");
        return Ok(new { success = true, price = Math.Round(price, 2), nisab, fetchedAt });
    }

    // GET /zakat/gold-price-by-date?date=2026-02-01
    [HttpGet("gold-price-by-date")]
    public async Task<IActionResult> GoldPriceByDate([FromQuery] string date)
    {
        if (!IsAuth) return Unauthorized();
        if (!DateOnly.TryParse(date, out var dateOnly))
            return BadRequest(new { found = false });

        var snapshot = await _gold.GetByDateAsync(dateOnly);
        if (snapshot == null)
            return Ok(new { found = false });

        var nisab = Math.Round(snapshot.PriceGram21K * 85, 2);
        return Ok(new { found = true, price = snapshot.PriceGram21K, nisab,
            date = snapshot.PriceDate.ToString("yyyy/MM/dd") });
    }

    // GET /zakat/history
    [HttpGet("history")]
    public async Task<IActionResult> History()
    {
        if (!IsAuth) return Unauthorized();
        var records = await _zakat.GetUserRecordsAsync(UserId);
        var result = records.Select(r => new
        {
            id               = r.Id,
            entryDate        = r.EntryDate.ToString("yyyy-MM-dd"),
            hijriDate        = r.HijriDate,
            cashBalance      = r.CashBalance,
            stocksMarketValue= r.StocksMarketValue,
            totalPortfolio   = r.TotalPortfolio,
            nisabValue       = r.NisabValue,
            goldGramPrice    = r.GoldGramPrice21K,
            reachedNisab     = r.ReachedNisab,
            zakatAmount      = r.ZakatAmount,
            zakatRate        = r.ZakatRate,
            status           = (int)r.Status,
            notes            = r.Notes
        });
        return Ok(result);
    }

    // POST /zakat/save
    [HttpPost("save")]
    public async Task<IActionResult> Save([FromBody] ZakatSaveRequest req)
    {
        if (!IsAuth) return Unauthorized();
        if (req == null) return BadRequest();

        var record = new ZakatRecord
        {
            UserId           = UserId,
            CashBalance      = req.CashBalance,
            StocksMarketValue= req.StocksMarketValue,
            TotalPortfolio   = req.CashBalance + req.StocksMarketValue,
            GoldGramPrice21K = req.GoldGramPrice21K,
            NisabValue       = Math.Round(req.GoldGramPrice21K * 85, 2),
            ReachedNisab     = (req.CashBalance + req.StocksMarketValue) >= Math.Round(req.GoldGramPrice21K * 85, 2),
            ZakatRate        = 2.5m,
            Status           = req.Status.HasValue ? (ZakatEntryStatus)req.Status.Value : ZakatEntryStatus.NotSet,
            EntryDate        = req.EntryDate,
            HijriDate        = req.HijriDate,
            Notes            = req.Notes
        };

        var total = record.TotalPortfolio;
        record.ZakatAmount = record.ReachedNisab ? Math.Round(total * 0.025m, 2) : 0m;

        var saved = await _zakat.SaveRecordAsync(record);
        return Ok(new { id = saved.Id });
    }

    // PUT /zakat/{id}
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ZakatSaveRequest req)
    {
        if (!IsAuth) return Unauthorized();
        if (req == null) return BadRequest();

        var nisab = Math.Round(req.GoldGramPrice21K * 85, 2);
        var total  = req.CashBalance + req.StocksMarketValue;
        var reached = total >= nisab;

        var updated = new ZakatRecord
        {
            CashBalance       = req.CashBalance,
            StocksMarketValue = req.StocksMarketValue,
            TotalPortfolio    = total,
            GoldGramPrice21K  = req.GoldGramPrice21K,
            NisabValue        = nisab,
            ReachedNisab      = reached,
            ZakatRate         = 2.5m,
            ZakatAmount       = reached ? Math.Round(total * 0.025m, 2) : 0m,
            Status            = req.Status.HasValue ? (ZakatEntryStatus)req.Status.Value : ZakatEntryStatus.NotSet,
            EntryDate         = req.EntryDate,
            HijriDate         = req.HijriDate,
            Notes             = req.Notes
        };

        var ok = await _zakat.UpdateRecordAsync(id, UserId, updated);
        return ok ? Ok() : NotFound();
    }

    // DELETE /zakat/{id}  (AJAX from history table)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAuth) return Unauthorized();
        var deleted = await _zakat.DeleteRecordAsync(id, UserId);
        return deleted ? Ok() : NotFound();
    }

    // POST /zakat/{id}/delete  (form submit from detail page)
    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePost(int id)
    {
        if (!IsAuth) return Redirect("/account/login");
        await _zakat.DeleteRecordAsync(id, UserId);
        TempData["Success"] = "تم حذف السجل";
        return Redirect("/zakat");
    }
}

public class ZakatSaveRequest
{
    public decimal  CashBalance       { get; set; }
    public decimal  StocksMarketValue { get; set; }
    public decimal  GoldGramPrice21K  { get; set; }
    public int?     Status            { get; set; }
    public DateTime EntryDate         { get; set; }
    public string?  HijriDate         { get; set; }
    public string?  Notes             { get; set; }
}
