using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HalalStocks.Controllers.Admin;

// Rendered with Html.Raw on the public calculator pages (same trust model as article content),
// so editing is limited to SuperAdmin/Admin rather than Editor.
[Authorize(Roles = "SuperAdmin,Admin")]
[Route("admin/instructions")]
public class CalculatorInstructionsController : Controller
{
    private const long MaxImageBytes = 5 * 1024 * 1024;

    private readonly ICalculatorInstructionService _instructions;
    private readonly IStorageService               _storage;

    public CalculatorInstructionsController(ICalculatorInstructionService instructions, IStorageService storage)
    {
        _instructions = instructions;
        _storage      = storage;
    }

    // GET /admin/instructions?type=DividendPurification
    [Route("")]
    public async Task<IActionResult> Index([FromQuery] CalculatorType? type)
    {
        var selected = type is { } t && Enum.IsDefined(t) ? t : CalculatorType.CapitalPurification;
        var vm = new CalculatorInstructionFormViewModel
        {
            Type        = selected,
            ContentHtml = await _instructions.GetContentAsync(selected)
        };
        return View("~/Views/Admin/CalculatorInstructions/Index.cshtml", vm);
    }

    [HttpPost, Route("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CalculatorInstructionFormViewModel vm)
    {
        if (!Enum.IsDefined(vm.Type))
            ModelState.AddModelError(nameof(vm.Type), "يرجى اختيار الحاسبة.");

        if (!ModelState.IsValid)
            return View("~/Views/Admin/CalculatorInstructions/Index.cshtml", vm);

        await _instructions.SaveContentAsync(vm.Type, vm.ContentHtml);
        TempData["Success"] = "تم حفظ الإرشادات بنجاح";
        return RedirectToAction(nameof(Index), new { type = vm.Type });
    }

    // POST /admin/instructions/upload-image — inline images for the editor. The editor inserts the
    // returned URL; images are never embedded as base64 (that would blow past the content length limit).
    [HttpPost, Route("upload-image")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadImage(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "لم يتم اختيار صورة." });

        if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "الملف المختار ليس صورة." });

        // The storage service enforces the extension whitelist (jpg/jpeg/png/webp — no SVG) and the size cap.
        var url = await _storage.UploadAsync(file, "instructions", maxBytes: MaxImageBytes);
        if (url == null)
            return BadRequest(new { error = "تعذّر رفع الصورة. الصيغ المسموحة: JPG و PNG و WEBP، والحد الأقصى 5 ميجا." });

        return Json(new { url });
    }
}
