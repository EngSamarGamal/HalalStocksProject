namespace HalalStocks.Models;

// Stored as int — never renumber.
public enum FatwaStatus
{
    Pending = 1,    // في انتظار الرد
    Answered = 2,   // تمت الإجابة (غير منشور)
    Published = 3   // منشور للعامة
}

public class FatwaQuestion
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    // Quill HTML written by an admin; rendered with Html.Raw (same trust model as article content).
    public string? Answer { get; set; }

    // Null once the asking user is deleted — the Q&A itself survives.
    public string? UserId { get; set; }
    public AppUser? User { get; set; }

    public FatwaStatus Status { get; set; } = FatwaStatus.Pending;
    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AnsweredAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}
