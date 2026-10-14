using HalalStocks.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalalStocks.Controllers.Admin;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
[Route("admin/gold-prices")]
public class GoldPricesController : Controller
{
    private readonly IGoldPriceService _gold;

    public GoldPricesController(IGoldPriceService gold) => _gold = gold;

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var snapshots = await _gold.GetAllSnapshotsAsync();
        return View("~/Views/Admin/GoldPrices/Index.cshtml", snapshots);
    }

    [HttpPost("save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(DateOnly date, decimal price)
    {
        if (price <= 0)
        {
            TempData["Error"] = "السعر يجب أن يكون أكبر من صفر.";
            return Redirect("/admin/gold-prices");
        }

        await _gold.SaveManualPriceAsync(date, price);
        TempData["Success"] = $"تم حفظ سعر الذهب ليوم {date:yyyy/MM/dd}: {price:N2} ج.م";
        return Redirect("/admin/gold-prices");
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _gold.DeleteAsync(id);
        TempData[deleted ? "Success" : "Error"] = deleted ? "تم حذف السجل." : "السجل غير موجود.";
        return Redirect("/admin/gold-prices");
    }
}
