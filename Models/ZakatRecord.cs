namespace HalalStocks.Models;

public class ZakatRecord
{
    public int    Id     { get; set; }
    public string UserId { get; set; } = "";
    public AppUser User  { get; set; } = null!;

    // Inputs
    public decimal CashBalance        { get; set; }
    public decimal StocksMarketValue  { get; set; }
    public decimal TotalPortfolio     { get; set; }

    // Gold Nisab
    public decimal GoldGramPrice21K   { get; set; }
    public decimal NisabValue         { get; set; }

    // Result
    public bool     ReachedNisab  { get; set; }
    public decimal? ZakatAmount   { get; set; }
    public decimal  ZakatRate     { get; set; } = 2.5m;

    // Status chosen by user
    public ZakatEntryStatus Status { get; set; }

    // Dates
    public DateTime EntryDate  { get; set; }
    public string?  HijriDate  { get; set; }

    public string?  Notes     { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum ZakatEntryStatus
{
    ReachedNisabAndStartedHawl = 0,  // بلوغ النصاب و بدء الحول
    DidNotReachNisab           = 1,  // لم يبلغ النصاب
    NewHawlStarted             = 2,  // بداية حول جديد
    HawlEnded                  = 3,  // انتهاء الحول
    ContinuingPreviousHawl     = 4,  // متابعة سريان الحول
    NotSet                     = 5   // غير محدد
}
