namespace HalalStocks.Models;

/// <summary>
/// Public-facing authoring identity for a user with AppUser.IsAnalyst == true.
/// One-to-one with AppUser (HasIndex(UserId).IsUnique() in AppDbContext).
/// </summary>
public class AnalystProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
    public string? SummaryAr { get; set; }
    public string? PhotoPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
