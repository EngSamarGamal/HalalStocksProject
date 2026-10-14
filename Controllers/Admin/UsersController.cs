using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;

namespace HalalStocks.Controllers.Admin;

[Authorize(Roles = "SuperAdmin,Admin,Editor")]
[Route("admin/users")]
public class UsersController : Controller
{
    private readonly IUserService _userService;
    public UsersController(IUserService userService) => _userService = userService;

    [Route("")]
    public async Task<IActionResult> Index(string tab = "admin")
    {
        var adminUsers  = await _userService.GetAdminUsersAsync();
        var publicUsers = await _userService.GetPublicUsersAsync();

        var vm = new UsersIndexViewModel
        {
            Tab         = tab,
            Users       = tab == "public" ? publicUsers : adminUsers,
            AdminCount  = adminUsers.Count(),
            PublicCount = publicUsers.Count()
        };
        return View("~/Views/Admin/Users/Index.cshtml", vm);
    }

    [Route("create")]
    public IActionResult Create() =>
        View("~/Views/Admin/Users/Create.cshtml", new UserFormViewModel());

    [HttpPost, Route("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/Users/Create.cshtml", vm);

        var user = new AppUser
        {
            FullName = vm.FullName,
            Email = vm.Email,
            UserName = vm.Email,
            Role = vm.Role,
            EmailConfirmed = true
        };
        var (success, errors) = await _userService.CreateAsync(user, vm.Password!);
        if (!success)
        {
            foreach (var err in errors) ModelState.AddModelError("", err);
            return View("~/Views/Admin/Users/Create.cshtml", vm);
        }
        TempData["Success"] = "تم إنشاء المستخدم بنجاح";
        return Redirect("/admin/users");
    }

    [Route("edit/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound();

        var isPublic = user.Role == UserRole.User;
        var vm = new AdminEditUserViewModel
        {
            Id                   = user.Id,
            IsPublicUser         = isPublic,
            UserName             = user.UserName ?? "",
            FullName             = user.FullName ?? "",
            FirstName            = user.FirstName ?? "",
            LastName             = user.LastName ?? "",
            Email                = user.Email ?? "",
            PhoneNumber          = user.PhoneNumber ?? "",
            InterestedInPaidVersion = user.InterestedInPaidVersion,
            Role                 = user.Role,
            IsAnalyst            = user.IsAnalyst
        };
        return View("~/Views/Admin/Users/Edit.cshtml", vm);
    }

    [HttpPost, Route("edit/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, AdminEditUserViewModel vm)
    {
        if (!ModelState.IsValid)
            return View("~/Views/Admin/Users/Edit.cshtml", vm);

        var (success, errors) = await _userService.UpdateUserAsync(
            id,
            vm.IsPublicUser ? null : vm.FullName,
            vm.IsPublicUser ? vm.FirstName : null,
            vm.IsPublicUser ? vm.LastName  : null,
            vm.Email,
            vm.IsPublicUser ? vm.PhoneNumber : null,
            vm.IsPublicUser ? vm.InterestedInPaidVersion : null,
            vm.IsPublicUser ? null : vm.Role);

        if (!success)
        {
            foreach (var err in errors) ModelState.AddModelError("", err);
            return View("~/Views/Admin/Users/Edit.cshtml", vm);
        }

        TempData["Success"] = "تم تحديث بيانات المستخدم بنجاح";
        return Redirect($"/admin/users/edit/{id}");
    }

    [HttpPost, Route("reset-password/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id, string newPassword, string confirmNewPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword != confirmNewPassword)
        {
            TempData["PasswordError"] = newPassword != confirmNewPassword
                ? "كلمتا المرور غير متطابقتين"
                : "كلمة المرور مطلوبة";
            return Redirect($"/admin/users/edit/{id}");
        }

        var (success, errors) = await _userService.ResetPasswordAsync(id, newPassword);
        if (!success)
            TempData["PasswordError"] = string.Join(" | ", errors);
        else
            TempData["PasswordSuccess"] = "تم إعادة تعيين كلمة المرور بنجاح";

        return Redirect($"/admin/users/edit/{id}");
    }

    [HttpPost, Route("edit-role/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRole(string id, UserRole role)
    {
        await _userService.UpdateRoleAsync(id, role);
        TempData["Success"] = "تم تحديث الدور";
        return Redirect("/admin/users");
    }

    [HttpPost, Route("toggle-analyst/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAnalyst(string id)
    {
        var (success, errors) = await _userService.ToggleAnalystAsync(id);
        if (!success) TempData["Error"] = string.Join(" | ", errors);
        else TempData["Success"] = "تم تحديث صفة المحلل";
        return Redirect($"/admin/users/edit/{id}");
    }

    [HttpPost, Route("delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (success, error) = await _userService.DeleteAsync(id, currentUserId);
        if (!success) TempData["Error"] = error;
        else TempData["Success"] = "تم حذف المستخدم";
        return Redirect("/admin/users");
    }
}
