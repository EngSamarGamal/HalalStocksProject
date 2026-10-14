using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

public interface IUserService
{
    Task<IEnumerable<AppUser>> GetAllAsync();
    Task<IEnumerable<AppUser>> GetAdminUsersAsync();
    Task<IEnumerable<AppUser>> GetPublicUsersAsync();
    Task<AppUser?> GetByIdAsync(string id);
    Task<(bool Success, IEnumerable<string> Errors)> CreateAsync(AppUser user, string password);
    Task<(bool Success, IEnumerable<string> Errors)> UpdateRoleAsync(string id, UserRole role);

    // Toggles AppUser.IsAnalyst. On first designation (false -> true) this also creates the
    // user's AnalystProfile row, so the analyst self-service area (Phase 2) always has a
    // profile to work with. Note: the claims-based "Analyst" role only takes effect on the
    // user's next sign-in — see AppUserClaimsPrincipalFactory.
    Task<(bool Success, IEnumerable<string> Errors)> ToggleAnalystAsync(string id);

    // Explicit, idempotent "set to false" used by the admin analytics section (as opposed
    // to ToggleAnalystAsync's flip, which is only safe when the caller already knows the
    // current state). Existing articles they authored are untouched — they simply lose
    // access to /analyst self-service; admin analytics still shows and manages them.
    Task<(bool Success, IEnumerable<string> Errors)> RemoveAnalystDesignationAsync(string id);

    Task<(bool Success, string Error)> DeleteAsync(string id, string currentUserId);
    Task<(bool Success, IEnumerable<string> Errors)> UpdateUserAsync(string id, string? fullName, string? firstName, string? lastName, string email, string? phoneNumber, bool? interestedInPaid, UserRole? role);
    Task<(bool Success, IEnumerable<string> Errors)> ResetPasswordAsync(string id, string newPassword);
    Task<int> GetCountAsync();
}
