using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HalalStocks.Controllers;

public class CapitalPurificationSaveRequest
{
    public int  StockId     { get; set; }
    public long OwnedShares { get; set; }
    public int  HoldingDays { get; set; }
}

[Route("capital-purification")]
public class CapitalPurificationController : Controller
{
    private readonly ICapitalPurificationService _records;
    private readonly IStockService               _stocks;
    private readonly ICalculatorInstructionService _instructions;

    public CapitalPurificationController(ICapitalPurificationService records, IStockService stocks,
        ICalculatorInstructionService instructions)
    {
        _records      = records;
        _stocks       = stocks;
        _instructions = instructions;
    }

    private bool   IsAuth => User.Identity?.IsAuthenticated == true;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET /capital-purification  (optional ?edit=id to pre-load edit mode)
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] int? edit = null)
    {
        if (!IsAuth) return View("~/Views/Calculator/CapitalPurificationGuestGate.cshtml");
        ViewBag.EditId = edit;
        ViewBag.Instructions = await _instructions.GetContentAsync(CalculatorType.CapitalPurification);
        return View("~/Views/Calculator/CapitalPurification.cshtml");
    }

    // GET /api/stocks/capital-search?q=  — picker suggestions, with the figures the calculator needs
    [HttpGet("/api/stocks/capital-search")]
    public async Task<IActionResult> Search([FromQuery] string? q)
    {
        if (!IsAuth) return Unauthorized();

        var stocks = await _stocks.SearchActiveStocksAsync(q, take: 20);
        return Json(stocks.Select(s => new
        {
            id          = s.Id,
            ticker      = s.TickerSymbol,
            nameAr      = s.NameAr,
            totalShares = s.TotalCompanyShares,
            totalAssets = s.TotalCompanyAssets,
            ribaRatio   = s.NonShariahInvestmentRatio
        }));
    }

    // GET /capital-purification/records  — feeds both tables
    [HttpGet("records")]
    public async Task<IActionResult> Records()
    {
        if (!IsAuth) return Unauthorized();

        var records = await _records.GetUserRecordsAsync(UserId);
        return Json(records.Select(r => new
        {
            id                 = r.Id,
            stockId            = r.StockId,
            ticker             = r.Ticker,
            nameAr             = r.NameAr,
            ownedShares        = r.OwnedShares,
            holdingDays        = r.HoldingDays,
            totalCompanyShares = r.TotalCompanyShares,
            ownershipPercent   = r.OwnershipPercent,
            totalCompanyAssets = r.TotalCompanyAssets,
            capitalShare       = r.CapitalShare,
            ribaActivityRatio  = r.RibaActivityRatio,
            purificationAmount = r.PurificationAmount,
            isPaid             = r.IsPaid,
            createdAt          = r.CreatedAt
        }));
    }

    // POST /capital-purification/save
    [HttpPost("save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save([FromBody] CapitalPurificationSaveRequest req)
    {
        if (!IsAuth) return Unauthorized();

        var (record, error) = await BuildRecordAsync(req);
        if (error != null) return BadRequest(new { error });

        record!.UserId = UserId;
        var saved = await _records.SaveRecordAsync(record);
        return Ok(new { id = saved.Id });
    }

    // PUT /capital-purification/{id}
    [HttpPut("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, [FromBody] CapitalPurificationSaveRequest req)
    {
        if (!IsAuth) return Unauthorized();

        var (record, error) = await BuildRecordAsync(req);
        if (error != null) return BadRequest(new { error });

        var ok = await _records.UpdateRecordAsync(id, UserId, record!);
        return ok ? Ok() : NotFound();
    }

    // POST /capital-purification/{id}/delete
    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAuth) return Unauthorized();
        var deleted = await _records.DeleteRecordAsync(id, UserId);
        return deleted ? Ok() : NotFound();
    }

    // POST /capital-purification/{id}/toggle-paid  — moves the row between the two tables
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
    /// the stock and the share count, never any totals.
    /// </summary>
    private async Task<(CapitalPurificationRecord? Record, string? Error)> BuildRecordAsync(CapitalPurificationSaveRequest req)
    {
        if (req == null || req.StockId <= 0)
            return (null, "يرجى اختيار السهم أولاً.");

        if (req.OwnedShares <= 0)
            return (null, "يرجى إدخال عدد الأسهم التي تمتلكها.");

        if (req.HoldingDays <= 0)
            return (null, "يرجى إدخال عدد أيام الاحتفاظ بالسهم.");

        // ~100 years — a typo guard, not a business rule; holding beyond a year is legitimate.
        if (req.HoldingDays > 36500)
            return (null, "عدد أيام الاحتفاظ بالسهم غير منطقي.");

        var stock = await _stocks.GetByIdAsync(req.StockId);
        if (stock == null)
            return (null, "السهم المختار غير موجود.");

        var missing = new List<string>();
        if (stock.TotalCompanyShares is null or <= 0)  missing.Add("إجمالي أسهم الشركة");
        if (stock.TotalCompanyAssets is null)          missing.Add("إجمالي أصول الشركة");
        if (stock.NonShariahInvestmentRatio is null)   missing.Add("نسبة الأنشطة الربوية");
        if (missing.Count > 0)
            return (null, $"لا يمكن إتمام الحساب: البيانات التالية غير متوفرة لهذا السهم — {string.Join("، ", missing)}.");

        var totalShares = stock.TotalCompanyShares!.Value;
        if (req.OwnedShares > totalShares)
            return (null, "عدد الأسهم التي تمتلكها أكبر من إجمالي أسهم الشركة.");

        var totalAssets = stock.TotalCompanyAssets!.Value;
        var ribaRatio   = stock.NonShariahInvestmentRatio!.Value;

        var ownershipPercent = (decimal)req.OwnedShares / totalShares * 100m;
        var capitalShare     = Math.Round(totalAssets * ownershipPercent / 100m, 2, MidpointRounding.AwayFromZero);

        // The annual basis is rounded once, then pro-rated by how long the shares were held.
        var annual       = Math.Round(capitalShare * ribaRatio / 100m, 2, MidpointRounding.AwayFromZero);
        var purification = Math.Round(annual * req.HoldingDays / 365m, 2, MidpointRounding.AwayFromZero);

        return (new CapitalPurificationRecord
        {
            StockId            = stock.Id,
            Ticker             = stock.TickerSymbol,
            NameAr             = stock.NameAr,
            OwnedShares        = req.OwnedShares,
            HoldingDays        = req.HoldingDays,
            TotalCompanyShares = totalShares,
            OwnershipPercent   = Math.Round(ownershipPercent, 8, MidpointRounding.AwayFromZero),
            TotalCompanyAssets = totalAssets,
            CapitalShare       = capitalShare,
            RibaActivityRatio  = ribaRatio,
            PurificationAmount = purification
        }, null);
    }
}
