using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalalStocks.Controllers.Admin;

// Answers are rendered with Html.Raw on public pages, so this carries the same role gate as articles.
[Authorize(Roles = "SuperAdmin,Admin,Editor")]
[Route("admin/fatawa")]
public class FatawaController : Controller
{
    private readonly IFatwaService _fatwaService;
    private const int PageSize = 20;

    public FatawaController(IFatwaService fatwaService) => _fatwaService = fatwaService;

    [HttpGet("")]
    public async Task<IActionResult> Index(FatwaStatus? status, int page = 1)
    {
        var (items, total) = await _fatwaService.GetForAdminAsync(status, page, PageSize);
        var counts = await _fatwaService.GetCountsAsync();
        return View("~/Views/Admin/Fatawa/Index.cshtml", new FatwaListViewModel
        {
            Questions = items,
            Status = status,
            AllCount = counts.All,
            PendingCount = counts.Pending,
            AnsweredCount = counts.Answered,
            PublishedCount = counts.Published,
            CurrentPage = Math.Max(page, 1),
            TotalPages = (int)Math.Ceiling(total / (double)PageSize)
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        var q = await _fatwaService.GetByIdAsync(id);
        if (q == null) return NotFound();
        return View("~/Views/Admin/Fatawa/Edit.cshtml", ToViewModel(q));
    }

    [HttpPost("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, FatwaAnswerFormViewModel vm)
    {
        var q = await _fatwaService.GetByIdAsync(id);
        if (q == null) return NotFound();

        var hasAnswer = !string.IsNullOrWhiteSpace(vm.AnswerHtml);
        if (vm.IsPublished && !hasAnswer)
            ModelState.AddModelError(nameof(vm.AnswerHtml), "اكتب الإجابة أولاً قبل نشر السؤال");

        if (!ModelState.IsValid)
        {
            // Read-only context comes from the DB, never from the posted form.
            var fresh = ToViewModel(q);
            fresh.AnswerHtml = vm.AnswerHtml;
            fresh.IsPublished = vm.IsPublished;
            fresh.IsFeatured = vm.IsFeatured;
            return View("~/Views/Admin/Fatawa/Edit.cshtml", fresh);
        }

        await _fatwaService.SaveAnswerAsync(id, vm.AnswerHtml, vm.IsPublished, vm.IsFeatured);
        TempData["Success"] = "تم حفظ الإجابة بنجاح";
        return Redirect("/admin/fatawa");
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        await _fatwaService.DeleteAsync(id);
        TempData["Success"] = "تم حذف السؤال";
        return Redirect("/admin/fatawa");
    }

    private static FatwaAnswerFormViewModel ToViewModel(FatwaQuestion q) => new()
    {
        Id = q.Id,
        Question = q.Question,
        AskerName = q.User == null
            ? "مستخدم محذوف"
            : (!string.IsNullOrWhiteSpace(q.User.FirstName) ? $"{q.User.FirstName} {q.User.LastName}".Trim() : q.User.UserName ?? ""),
        AskerEmail = q.User?.Email,
        AskerPhone = q.User?.PhoneNumber,
        CreatedAt = q.CreatedAt,
        Status = q.Status,
        AnswerHtml = q.Answer,
        IsPublished = q.IsPublished,
        IsFeatured = q.IsFeatured
    };
}
