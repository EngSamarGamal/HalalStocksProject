using HalalStocks.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace HalalStocks.Services;

/// <summary>
/// Adds the custom AppUser.Role enum value as a ClaimTypes.Role claim so that
/// [Authorize(Roles = "SuperAdmin,Admin,Editor")] works without using IdentityRole tables.
/// </summary>
public class AppUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<AppUser>
{
    public AppUserClaimsPrincipalFactory(
        UserManager<AppUser> userManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, optionsAccessor) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        // Add the enum name (e.g. "SuperAdmin", "Admin", "Editor", "User") as a role claim
        identity.AddClaim(new Claim(ClaimTypes.Role, user.Role.ToString()));
        // IsAnalyst is orthogonal to Role (an Admin can also be an analyst), so it's a
        // second, independent role claim rather than a UserRole enum value. Note this is
        // baked into the auth cookie at sign-in — toggling IsAnalyst only takes effect on
        // the user's next login/refresh, so analyst-area actions must re-check the DB flag
        // directly rather than trusting this claim alone.
        if (user.IsAnalyst)
            identity.AddClaim(new Claim(ClaimTypes.Role, "Analyst"));
        return identity;
    }
}
