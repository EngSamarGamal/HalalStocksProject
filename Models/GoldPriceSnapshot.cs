namespace HalalStocks.Models;

public class GoldPriceSnapshot
{
    public int      Id           { get; set; }
    public DateOnly PriceDate    { get; set; }
    public decimal  PriceGram21K { get; set; }
    public string   Source       { get; set; } = "Manual"; // "Manual" or "Api"
    public DateTime FetchedAt    { get; set; } = DateTime.UtcNow;
}
