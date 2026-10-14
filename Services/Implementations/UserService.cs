using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Services.Implementations;

public class UserService : IUserService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _db;
    public UserService(UserManager<AppUser> userManager, AppDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<IEnumerable<AppUser>> GetAllAsync() =>
        await _userManager.Users.OrderBy(u => u.FullName).ToListAsync();

    public async Task<IEnumerable<AppUser>> GetAdminUsersAsync() =>
        await _userManager.Users
            .Where(u => u.Role == UserRole.SuperAdmin || u.Role == UserRole.Admin || u.Role == UserRole.Editor)
            .OrderBy(u => u.FullName).ToListAsync();

    public async Task<IEnumerable<AppUser>> GetPublicUsersAsync() =>
        await _userManager.Users
            .Where(u => u.Role == UserRole.User)
            .OrderByDescending(u => u.CreatedAt).ToListAsync();

    public async Task<AppUser?> GetByIdAsync(string id) =>
        await _userManager.FindByIdAsync(id);

    public async Task<(bool Success, IEnumerable<string> Errors)> CreateAsync(AppUser user, string password)
    {
        var result = await _userManager.CreateAsync(user, password);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateRoleAsync(string id, UserRole role)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return (false, new[] { "المستخدم غير موجود" });
        user.Role = role;
        var result = await _userManager.UpdateAsync(user);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> ToggleAnalystAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return (false, new[] { "المستخدم غير موجود" });

        user.IsAnalyst = !user.IsAnalyst;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded) return (false, result.Errors.Select(e => e.Description));

        if (user.IsAnalyst && !await _db.AnalystProfiles.AnyAsync(p => p.UserId == id))
        {
            _db.AnalystProfiles.Add(new AnalystProfile { UserId = id });
            await _db.SaveChangesAsync();
        }

        return (true, Array.Empty<string>());
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> RemoveAnalystDesignationAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return (false, new[] { "المستخدم غير موجود" });
        if (!user.IsAnalyst) return (true, Array.Empty<string>()); // already not an analyst — no-op

        user.IsAnalyst = false;
        var result = await _userManager.UpdateAsync(user);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task<(bool Success, string Error)> DeleteAsync(string id, string currentUserId)
    {
        if (id == currentUserId) return (false, "لا يمكنك حذف حسابك الخاص");
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return (false, "المستخدم غير موجود");
        var result = await _userManager.DeleteAsync(user);
        return (result.Succeeded, result.Errors.FirstOrDefault()?.Description ?? "");
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> UpdateUserAsync(
        string id, string? fullName, string? firstName, string? lastName,
        string email, string? phoneNumber, bool? interestedInPaid, UserRole? role)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return (false, new[] { "المستخدم غير موجود" });

        var emailOwner = await _userManager.FindByEmailAsync(email);
        if (emailOwner != null && emailOwner.Id != id)
            return (false, new[] { "البريد الإلكتروني مستخدم بالفعل من حساب آخر" });

        if (fullName    != null) user.FullName               = fullName;
        if (firstName   != null) user.FirstName              = firstName;
        if (lastName    != null) user.LastName               = lastName;
        if (phoneNumber != null) user.PhoneNumber            = phoneNumber;
        if (interestedInPaid.HasValue) user.InterestedInPaidVersion = interestedInPaid.Value;
        if (role.HasValue)       user.Role                   = role.Value;

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            await _userManager.SetEmailAsync(user, email);

        var result = await _userManager.UpdateAsync(user);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task<(bool Success, IEnumerable<string> Errors)> ResetPasswordAsync(string id, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return (false, new[] { "المستخدم غير موجود" });

        var token  = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        return (result.Succeeded, result.Errors.Select(e => e.Description));
    }

    public async Task<int> GetCountAsync() => await _userManager.Users.CountAsync();
}
