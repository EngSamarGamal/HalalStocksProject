namespace HalalStocks.Models;

public class StockIndex
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string? Description { get; set; }

    public string? CommonName { get; set; }

    public ICollection<Stock> Stocks { get; set; } = new List<Stock>();
}
