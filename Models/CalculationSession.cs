namespace HalalStocks.Models;

public class CalculationSession
{
    public int     Id                { get; set; }
    public string  UserId            { get; set; } = "";
    public AppUser User              { get; set; } = null!;
    public string? Title             { get; set; }
    public DateTime CreatedAt        { get; set; } = DateTime.UtcNow;
    public decimal TotalProfit       { get; set; }
    public decimal TotalPurification { get; set; }
    public bool    IsPaid            { get; set; } = false;

    public ICollection<CalculationEntry> Entries { get; set; } = new List<CalculationEntry>();
}

public class CalculationEntry
{
    public int    Id           { get; set; }
    public int    SessionId    { get; set; }
    public CalculationSession Session { get; set; } = null!;
    public string Ticker       { get; set; } = "";
    public string NameAr       { get; set; } = "";
    public decimal Buy         { get; set; }
    public decimal Sell        { get; set; }
    public decimal Profit      { get; set; }
    public decimal Ratio       { get; set; }
    public decimal Purification { get; set; }

    // Optional fields
    public int?      StockQuantity     { get; set; }
    public DateTime? BuyDate           { get; set; }
    public string?   BuyInvoiceNumber  { get; set; }
    public DateTime? SellDate          { get; set; }
    public string?   SellInvoiceNumber { get; set; }
    public string?   Notes             { get; set; }
}
