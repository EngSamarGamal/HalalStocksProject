using Microsoft.AspNetCore.Identity;

namespace HalalStocks.Models;

public enum UserRole { SuperAdmin, Admin, Editor, User }

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Editor;

    // Orthogonal to Role — grants access to the analyst self-service area (AnalystProfile,
    // own-article authoring) independent of admin/public status, so an Admin can also be
    // an analyst and a public User keeps their normal UserRole.User standing.
    public bool IsAnalyst { get; set; }

    // Public user fields
    public string? FirstName { get; set; }
    public string? LastName  { get; set; }
    public bool InterestedInPaidVersion { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
