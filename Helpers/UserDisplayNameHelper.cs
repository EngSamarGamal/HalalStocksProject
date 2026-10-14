using HalalStocks.Models;

namespace HalalStocks.Helpers;

public static class UserDisplayNameHelper
{
    // Same fallback order as WalletManagementController.DisplayName: public users carry
    // FirstName/LastName, admin/editor accounts carry FullName instead.
    public static string DisplayName(AppUser user)
    {
        if (!string.IsNullOrWhiteSpace(user.FirstName) || !string.IsNullOrWhiteSpace(user.LastName))
            return $"{user.FirstName} {user.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(user.FullName)) return user.FullName;
        return user.UserName ?? "—";
    }
}
