using HalalStocks.Models;
using HalalStocks.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace HalalStocks.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private readonly UserManager<AppUser>   _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager   = userManager;
        _signInManager = signInManager;
    }

    // ── Register ──────────────────────────────────────────────────────────

    [HttpGet("register")]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect("/");
        ViewBag.ReturnUrl = returnUrl;
        return View("~/Views/Account/Register.cshtml");
    }

    [HttpPost("register")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel vm, string? returnUrl = null)
    {
        // Keep returnUrl alive across a failed attempt (same shape as Login).
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid)
            return View("~/Views/Account/Register.cshtml", vm);

        // Check username uniqueness
        if (await _userManager.FindByNameAsync(vm.UserName) != null)
        {
            ModelState.AddModelError("UserName", "اسم المستخدم مستخدم بالفعل، يرجى اختيار اسم آخر");
            return View("~/Views/Account/Register.cshtml", vm);
        }

        // Check email uniqueness
        if (await _userManager.FindByEmailAsync(vm.Email) != null)
        {
            ModelState.AddModelError("Email", "البريد الإلكتروني مستخدم بالفعل");
            return View("~/Views/Account/Register.cshtml", vm);
        }

        var user = new AppUser
        {
            UserName                = vm.UserName,
            Email                   = vm.Email,
            FirstName               = vm.FirstName,
            LastName                = vm.LastName,
            PhoneNumber             = vm.PhoneNumber,
            InterestedInPaidVersion = vm.InterestedInPaidVersion,
            Role                    = UserRole.User,
            EmailConfirmed          = true,
            CreatedAt               = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, vm.Password);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError("", err.Description);
            return View("~/Views/Account/Register.cshtml", vm);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);
        TempData["Success"] = "تم إنشاء حسابك بنجاح، مرحباً بك!";
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);
        return Redirect("/");
    }

    // ── Login ─────────────────────────────────────────────────────────────

    [HttpGet("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect("/");
        ViewBag.ReturnUrl = returnUrl;
        return View("~/Views/Account/Login.cshtml");
    }

    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        // Keep returnUrl alive across a failed attempt so the re-rendered form still carries it.
        ViewBag.ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
            return View("~/Views/Account/Login.cshtml", vm);

        var result = await _signInManager.PasswordSignInAsync(
            vm.UserName, vm.Password, vm.RememberMe, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "اسم المستخدم أو كلمة المرور غير صحيحة");
            return View("~/Views/Account/Login.cshtml", vm);
        }

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return Redirect("/");
    }

    // ── Logout ────────────────────────────────────────────────────────────

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Redirect("/");
    }

    // ── Profile ───────────────────────────────────────────────────────────

    [HttpGet("profile")]
    public async Task<IActionResult> Profile()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Redirect("/account/login");

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Redirect("/account/login");

        var vm = new ProfileViewModel
        {
            UserName                = user.UserName ?? "",
            FirstName               = user.FirstName ?? "",
            LastName                = user.LastName  ?? "",
            Email                   = user.Email     ?? "",
            PhoneNumber             = user.PhoneNumber ?? "",
            InterestedInPaidVersion = user.InterestedInPaidVersion,
            IsAnalyst               = user.IsAnalyst
        };
        return View("~/Views/Account/Profile.cshtml", vm);
    }

    [HttpPost("profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel vm)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Redirect("/account/login");

        if (!ModelState.IsValid)
            return View("~/Views/Account/Profile.cshtml", vm);

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Redirect("/account/login");

        // Check email uniqueness (exclude current user)
        var emailOwner = await _userManager.FindByEmailAsync(vm.Email);
        if (emailOwner != null && emailOwner.Id != user.Id)
        {
            ModelState.AddModelError("Email", "البريد الإلكتروني مستخدم بالفعل من حساب آخر");
            vm.UserName = user.UserName ?? "";
            return View("~/Views/Account/Profile.cshtml", vm);
        }

        user.FirstName               = vm.FirstName;
        user.LastName                = vm.LastName;
        user.PhoneNumber             = vm.PhoneNumber;
        user.InterestedInPaidVersion = vm.InterestedInPaidVersion;

        // Update email if changed
        if (!string.Equals(user.Email, vm.Email, StringComparison.OrdinalIgnoreCase))
        {
            await _userManager.SetEmailAsync(user, vm.Email);
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var err in result.Errors)
                ModelState.AddModelError("", err.Description);
            vm.UserName = user.UserName ?? "";
            return View("~/Views/Account/Profile.cshtml", vm);
        }

        TempData["Success"] = "تم تحديث بياناتك بنجاح";
        return Redirect("/account/profile");
    }

    // ── Change Password ───────────────────────────────────────────────────

    [HttpPost("change-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        if (User.Identity?.IsAuthenticated != true)
            return Redirect("/account/login");

        if (!ModelState.IsValid)
        {
            TempData["PasswordErrors"] = string.Join("|", ModelState.Values
                .SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return Redirect("/account/profile");
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Redirect("/account/login");

        var result = await _userManager.ChangePasswordAsync(user, vm.CurrentPassword, vm.NewPassword);
        if (!result.Succeeded)
        {
            TempData["PasswordErrors"] = string.Join("|", result.Errors.Select(e => e.Description));
            return Redirect("/account/profile");
        }

        // Refresh sign-in so the cookie stays valid after password change
        await _signInManager.RefreshSignInAsync(user);
        TempData["PasswordSuccess"] = "تم تغيير كلمة المرور بنجاح";
        return Redirect("/account/profile");
    }
}
