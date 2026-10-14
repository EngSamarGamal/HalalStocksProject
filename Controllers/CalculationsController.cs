using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HalalStocks.Controllers;

public class SaveSessionRequest
{
    public string?  Title   { get; set; }
    public List<CalculationEntry> Entries { get; set; } = new();
}

[Route("calculations")]
public class CalculationsController : Controller
{
    private readonly ICalculationService _svc;
    public CalculationsController(ICalculationService svc) => _svc = svc;

    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    // ── API: save ─────────────────────────────────────────────────────────

    [HttpPost("/api/calculations/save")]
    public async Task<IActionResult> Save([FromBody] SaveSessionRequest req)
    {
        if (User.Identity?.IsAuthenticated != true) return Unauthorized();
        if (req.Entries == null || req.Entries.Count == 0) return BadRequest();

        var session = await _svc.SaveAsync(UserId!, req.Title, req.Entries);
        return Ok(new { id = session.Id });
    }

    // ── History list ──────────────────────────────────────────────────────

    [Route("")]
    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Redirect("/account/login?returnUrl=/calculations");

        var sessions = await _svc.GetUserSessionsAsync(UserId!);
        return View("~/Views/Calculations/Index.cshtml", sessions);
    }

    // ── Session detail ────────────────────────────────────────────────────

    [Route("{id:int}")]
    public async Task<IActionResult> Detail(int id)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Redirect("/account/login");

        var session = await _svc.GetSessionAsync(id, UserId!);
        if (session == null) return NotFound();
        return View("~/Views/Calculations/Detail.cshtml", session);
    }

    // ── Delete ────────────────────────────────────────────────────────────

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (User.Identity?.IsAuthenticated != true) return Unauthorized();
        await _svc.DeleteAsync(id, UserId!);
        TempData["Success"] = "تم حذف الحساب";
        return Redirect("/calculations");
    }

    // ── Edit ──────────────────────────────────────────────────────────────

    [Route("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        if (User.Identity?.IsAuthenticated != true) return Redirect("/account/login");
        var session = await _svc.GetSessionAsync(id, UserId!);
        if (session == null) return NotFound();

        var vm = new EditCalculationViewModel
        {
            Id    = session.Id,
            Title = session.Title,
            Entries = session.Entries.Select(e => new EditEntryRow
            {
                Ticker              = e.Ticker,
                NameAr              = e.NameAr,
                Buy                 = e.Buy,
                Sell                = e.Sell,
                Ratio               = e.Ratio,
                StockQuantity       = e.StockQuantity,
                BuyDate             = e.BuyDate,
                BuyInvoiceNumber    = e.BuyInvoiceNumber,
                SellDate            = e.SellDate,
                SellInvoiceNumber   = e.SellInvoiceNumber,
                Notes               = e.Notes
            }).ToList()
        };
        return View("~/Views/Calculations/Edit.cshtml", vm);
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditCalculationViewModel vm)
    {
        if (User.Identity?.IsAuthenticated != true) return Redirect("/account/login");

        var entries = vm.Entries.Select(e =>
        {
            var profit       = e.Sell - e.Buy;
            var purification = profit > 0 ? profit * (e.Ratio / 100m) : 0m;
            return new CalculationEntry
            {
                Ticker              = e.Ticker,
                NameAr              = e.NameAr,
                Buy                 = e.Buy,
                Sell                = e.Sell,
                Profit              = profit,
                Ratio               = e.Ratio,
                Purification        = purification,
                StockQuantity       = e.StockQuantity,
                BuyDate             = e.BuyDate,
                BuyInvoiceNumber    = e.BuyInvoiceNumber,
                SellDate            = e.SellDate,
                SellInvoiceNumber   = e.SellInvoiceNumber,
                Notes               = e.Notes
            };
        });

        await _svc.UpdateAsync(id, UserId!, vm.Title, entries);
        TempData["Success"] = "تم تحديث الحساب بنجاح";
        return Redirect($"/calculations/{id}");
    }

    // ── Toggle paid ───────────────────────────────────────────────────────

    [HttpPost("{id:int}/toggle-paid")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TogglePaid(int id)
    {
        if (User.Identity?.IsAuthenticated != true) return Unauthorized();
        await _svc.TogglePaidAsync(id, UserId!);
        return Redirect($"/calculations/{id}");
    }
}
