using HalalStocks.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalalStocks.Controllers.Admin;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
[Route("admin/wallet-management-requests")]
public class WalletManagementRequestsController : Controller
{
    private readonly IWalletManagementRequestService _requestService;

    public WalletManagementRequestsController(IWalletManagementRequestService requestService) =>
        _requestService = requestService;

    [Route("")]
    public async Task<IActionResult> Index()
    {
        ViewBag.ApprovedCount    = await _requestService.GetApprovedCountAsync();
        ViewBag.PendingRequests  = (await _requestService.GetPendingAsync()).ToList();
        ViewBag.ApprovedRequests = (await _requestService.GetApprovedAsync()).ToList();
        return View("~/Views/Admin/WalletManagementRequests/Index.cshtml");
    }

    [HttpPost, Route("{id:int}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id)
    {
        if (!await _requestService.ApproveAsync(id))
            return NotFound();

        TempData["Success"] = "تم قبول طلب إدارة المحفظة";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Route("{id:int}/reject")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(int id)
    {
        if (!await _requestService.RejectAsync(id))
            return NotFound();

        TempData["Success"] = "تم رفض طلب إدارة المحفظة";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, Route("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!await _requestService.DeleteAsync(id))
            return NotFound();

        TempData["Success"] = "تم إلغاء صلاحية إدارة محفظة هذا المستخدم";
        return RedirectToAction(nameof(Index));
    }
}
