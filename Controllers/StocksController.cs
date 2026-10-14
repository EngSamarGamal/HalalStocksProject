using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace HalalStocks.Controllers;

public class StocksController : Controller
{
    private readonly IStockService _stockService;
    public StocksController(IStockService stockService) => _stockService = stockService;

    [Route("stocks/{ticker}")]
    public async Task<IActionResult> Detail(string ticker)
    {
        var stock = await _stockService.GetByTickerAsync(ticker);
        if (stock == null) return NotFound();

        ViewData["Canonical"] = "/stocks/" + stock.TickerSymbol.Trim();

        if (User.Identity?.IsAuthenticated != true)
            return View("~/Views/Stocks/GuestGate.cshtml", stock);

        return View(new StockDetailViewModel { Stock = stock });
    }
}
