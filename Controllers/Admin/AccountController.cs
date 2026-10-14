using HalalStocks.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HalalStocks.Controllers.Admin;

[Route("admin")]
public class AccountController : Controller
{
    private readonly SignInManager<AppUser> _signInManager;
    public AccountController(SignInManager<AppUser> signInManager) => _signInManager = signInManager;

    [Route("login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect("/admin");
        ViewBag.ReturnUrl = returnUrl;
        return View("~/Views/Admin/Account/Login.cshtml");
    }

    [HttpPost, Route("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string password, string? returnUrl = null)
    {
        var result = await _signInManager.PasswordSignInAsync(email, password, isPersistent: true, lockoutOnFailure: false);
        if (result.Succeeded)
            return LocalRedirect(returnUrl ?? "/admin");
        ModelState.AddModelError("", "البريد الإلكتروني أو كلمة المرور غير صحيحة");
        ViewBag.ReturnUrl = returnUrl;
        return View("~/Views/Admin/Account/Login.cshtml");
    }

    [HttpPost, Route("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home", new { area = "" });
    }
}
