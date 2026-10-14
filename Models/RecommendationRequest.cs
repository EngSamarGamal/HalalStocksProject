namespace HalalStocks.Models;

public enum RecommendationRequestStatus
{
    Pending,   // قيد المراجعة
    Approved,  // مقبول
    Rejected   // مرفوض
}

public class RecommendationRequest
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public RecommendationRequestStatus Status { get; set; } = RecommendationRequestStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
