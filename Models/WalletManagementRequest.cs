namespace HalalStocks.Models;

public enum WalletManagementRequestStatus
{
    Pending,   // قيد المراجعة
    Approved,  // مقبول
    Rejected   // مرفوض
}

public class WalletManagementRequest
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public WalletManagementRequestStatus Status { get; set; } = WalletManagementRequestStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
