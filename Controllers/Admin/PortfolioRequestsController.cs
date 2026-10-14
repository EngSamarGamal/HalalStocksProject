using HalalStocks.Data;
using HalalStocks.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Controllers.Admin;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
[Route("admin/portfolio-requests")]
public class PortfolioRequestsController : Controller
{
    private readonly AppDbContext _db;
    public PortfolioRequestsController(AppDbContext db) => _db = db;

    [Route("")]
    public async Task<IActionResult> Index(PortfolioRequestStatus? status)
    {
        var query = _db.PortfolioRequests.Include(r => r.User).AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        var requests = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();

        ViewBag.StatusFilter = status;
        return View("~/Views/Admin/PortfolioRequests/Index.cshtml", requests);
    }

    [Route("{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var request = await _db.PortfolioRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (request == null) return NotFound();

        return View("~/Views/Admin/PortfolioRequests/Details.cshtml", request);
    }

    [HttpPost, Route("{id}/update-status")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, PortfolioRequestStatus status, string? adminNotes)
    {
        var request = await _db.PortfolioRequests.FindAsync(id);
        if (request == null) return NotFound();

        request.Status     = status;
        request.AdminNotes = adminNotes;
        request.UpdatedAt  = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        TempData["Success"] = "تم تحديث حالة الطلب";
        return RedirectToAction(nameof(Details), new { id });
    }
}
