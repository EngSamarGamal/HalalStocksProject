namespace HalalStocks.Models;

/// <summary>
/// One saved capital-purification calculation. Every stock figure used is snapshotted
/// alongside the StockId so a later admin edit to the stock never rewrites saved history.
/// </summary>
public class CapitalPurificationRecord
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

    public long    OwnedShares        { get; set; }
    public int     HoldingDays        { get; set; }
    public long    TotalCompanyShares { get; set; }
    public decimal OwnershipPercent   { get; set; }
    public decimal TotalCompanyAssets { get; set; }
    public decimal CapitalShare       { get; set; }
    public decimal RibaActivityRatio  { get; set; }
    public decimal PurificationAmount { get; set; }

    public bool     IsPaid    { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
