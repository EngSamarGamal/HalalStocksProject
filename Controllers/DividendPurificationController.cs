using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HalalStocks.Controllers;

public class DividendPurificationSaveRequest
{
    public int      StockId    { get; set; }
    public string   Type       { get; set; } = "";   // "cash" | "stock"
    public decimal? CashAmount { get; set; }
    public long?    Shares     { get; set; }
    public decimal? SharePrice { get; set; }
}

[Route("dividend-purification")]
public class DividendPurificationController : Controller
{
    // Typo guards, not business rules — they also keep shares × price inside decimal(18,2).
    private const decimal MaxAmount = 1_000_000_000_000m;
    private const long    MaxShares = 1_000_000_000_000;
    private const decimal MaxPrice  = 1_000_000_000m;

    private readonly IDividendPurificationService _records;
    private readonly IStockService                _stocks;
    private readonly ICalculatorInstructionService _instructions;

    public DividendPurificationController(IDividendPurificationService records, IStockService stocks,
        ICalculatorInstructionService instructions)
    {
        _records      = records;
        _stocks       = stocks;
        _instructions = instructions;
    }

    private bool   IsAuth => User.Identity?.IsAuthenticated == true;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET /dividend-purification
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        if (!IsAuth) return View("GuestGate");
        ViewBag.Instructions = await _instructions.GetContentAsync(CalculatorType.DividendPurification);
        return View();
    }

    // GET /api/stocks/dividend-search?q=  — picker suggestions with the figures the calculator shows
    [HttpGet("/api/stocks/dividend-search")]
    public async Task<IActionResult> Search([FromQuery] string? q)
    {
        if (!IsAuth) return Unauthorized();

        var stocks = (await _stocks.SearchActiveStocksAsync(q, take: 20)).ToList();
        var dates  = await _stocks.GetLatestReviewDatesAsync(stocks.Select(s => s.Id));

        return Json(stocks.Select(s => new
        {
            id             = s.Id,
            ticker         = s.TickerSymbol,
            nameAr         = s.NameAr,
            ratio          = s.PurificationRatio,
            statementsDate = dates.TryGetValue(s.Id, out var d) ? d.ToString("yyyy-MM-dd") : null
        }));
    }

    // GET /dividend-purification/records  — feeds both tables
    [HttpGet("records")]
    public async Task<IActionResult> Records()
    {
        if (!IsAuth) return Unauthorized();

        var records = await _records.GetUserRecordsAsync(UserId);
        return Json(records.Select(r => new
        {
            id                 = r.Id,
            ticker             = r.Ticker,
            nameAr             = r.NameAr,
            type               = r.DividendType == DividendType.Cash ? "cash" : "stock",
            shares             = r.Shares,
            sharePrice         = r.SharePrice,
            totalDividends     = r.TotalDividends,
            ratio              = r.PurificationRatio,
            purificationAmount = r.PurificationAmount,
            statementsDate     = r.StatementsDate?.ToString("yyyy-MM-dd"),
            isPaid             = r.IsPaid,
            createdAt          = DateTime.SpecifyKind(r.CreatedAt, DateTimeKind.Utc)
        }));
    }

    // POST /dividend-purification/save
    [HttpPost("save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([FromBody] DividendPurificationSaveRequest req)
    {
        if (!IsAuth) return Unauthorized();

        var (record, error) = await BuildRecordAsync(req);
        if (error != null) return BadRequest(new { error });

        record!.UserId = UserId;
        var saved = await _records.SaveRecordAsync(record);
        return Ok(new { id = saved.Id });
    }

    // POST /dividend-purification/{id}/delete
    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAuth) return Unauthorized();
        var deleted = await _records.DeleteRecordAsync(id, UserId);
        return deleted ? Ok() : NotFound();
    }

    // POST /dividend-purification/{id}/toggle-paid  — moves the row between the two tables
    [HttpPost("{id:int}/toggle-paid")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePaid(int id)
    {
        if (!IsAuth) return Unauthorized();
        var toggled = await _records.TogglePaidAsync(id, UserId);
        return toggled ? Ok() : NotFound();
    }

    /// <summary>
    /// Recomputes the whole calculation from the stock's current data — the client sends only
    /// the stock and the dividend inputs, never a total or a ratio.
    /// </summary>
    private async Task<(DividendPurificationRecord? Record, string? Error)> BuildRecordAsync(DividendPurificationSaveRequest req)
    {
        if (req == null || req.StockId <= 0)
            return (null, "يرجى اختيار السهم أولاً.");

        DividendType type;
        decimal total;
        long? shares = null;
        decimal? price = null;

        switch (req.Type)
        {
            case "cash":
                if (req.CashAmount is null or <= 0)
                    return (null, "يرجى إدخال قيمة التوزيعات النقدية.");
                if (req.CashAmount > MaxAmount)
                    return (null, "قيمة التوزيعات النقدية غير منطقية.");
                type  = DividendType.Cash;
                total = Math.Round(req.CashAmount.Value, 2, MidpointRounding.AwayFromZero);
                break;

            case "stock":
                if (req.Shares is null or <= 0)
                    return (null, "يرجى إدخال عدد الأسهم.");
                if (req.SharePrice is null or <= 0)
                    return (null, "يرجى إدخال سعر السهم.");
                if (req.Shares > MaxShares || req.SharePrice > MaxPrice)
                    return (null, "عدد الأسهم أو سعر السهم غير منطقي.");
                type   = DividendType.Stock;
                shares = req.Shares;
                price  = req.SharePrice;
                total  = Math.Round(req.Shares.Value * req.SharePrice.Value, 2, MidpointRounding.AwayFromZero);
                break;

            default:
                return (null, "يرجى اختيار نوع التوزيع.");
        }

        var stock = await _stocks.GetByIdAsync(req.StockId);
        if (stock == null)
            return (null, "السهم المختار غير موجود.");

        if (stock.PurificationRatio is null)
            return (null, "لا تتوفر نسبة تطهير لهذا السهم حالياً، لذلك لا يمكن حساب المبلغ.");

        var ratio        = stock.PurificationRatio.Value;
        var purification = Math.Round(total * ratio / 100m, 2, MidpointRounding.AwayFromZero);

        var dates = await _stocks.GetLatestReviewDatesAsync(new[] { stock.Id });

        return (new DividendPurificationRecord
        {
            StockId            = stock.Id,
            Ticker             = stock.TickerSymbol,
            NameAr             = stock.NameAr,
            StatementsDate     = dates.TryGetValue(stock.Id, out var d) ? d : null,
            DividendType       = type,
            Shares             = shares,
            SharePrice         = price,
            TotalDividends     = total,
            PurificationRatio  = ratio,
            PurificationAmount = purification
        }, null);
    }
}
