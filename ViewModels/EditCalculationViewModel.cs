namespace HalalStocks.ViewModels;

public class EditCalculationViewModel
{
    public int     Id      { get; set; }
    public string? Title   { get; set; }
    public List<EditEntryRow> Entries { get; set; } = new();
}

public class EditEntryRow
{
    public string  Ticker             { get; set; } = "";
    public string  NameAr             { get; set; } = "";
    public decimal Buy                { get; set; }
    public decimal Sell               { get; set; }
    public decimal Ratio              { get; set; }
    public int?      StockQuantity     { get; set; }
    public DateTime? BuyDate           { get; set; }
    public string?   BuyInvoiceNumber  { get; set; }
    public DateTime? SellDate          { get; set; }
    public string?   SellInvoiceNumber { get; set; }
    public string?   Notes             { get; set; }
}
