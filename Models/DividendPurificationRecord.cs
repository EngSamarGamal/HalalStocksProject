namespace HalalStocks.Models;

public enum DividendType { Cash = 0, Stock = 1 }

/// <summary>
/// One saved dividend-purification calculation. Every stock figure used is snapshotted
/// alongside the StockId so a later admin edit to the stock never rewrites saved history.
/// </summary>
public class DividendPurificationRecord
{
    public int      Id     { get; set; }
    public string   UserId { get; set; } = "";
    public AppUser  User   { get; set; } = null!;

    // Nullable so deleting a stock never destroys a user's saved record — the snapshot below
    // keeps the record fully readable on its own.
    public int?   StockId { get; set; }
    public Stock? Stock   { get; set; }
    public string Ticker  { get; set; } = "";
    public string NameAr  { get; set; } = "";

    // Latest financial-statements date known when the record was saved (nullable: some stocks have none).
    public DateTime? StatementsDate { get; set; }

    public DividendType DividendType { get; set; }

    // Only set for stock dividends (Shares × SharePrice = TotalDividends).
    public long?    Shares     { get; set; }
    public decimal? SharePrice { get; set; }

    // For a cash dividend this is the amount received; for a stock dividend, Shares × SharePrice.
    public decimal TotalDividends    { get; set; }
    public decimal PurificationRatio { get; set; }
    public decimal PurificationAmount { get; set; }

    public bool     IsPaid    { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
