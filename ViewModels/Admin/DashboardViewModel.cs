namespace HalalStocks.ViewModels.Admin;

public class DashboardViewModel
{
    public int TotalStocks { get; set; }
    public int TotalCategories { get; set; }
    public int TotalArticles { get; set; }
    public int TotalUsers { get; set; }
    public int TotalZakatRecords { get; set; }
    public int NewPortfolioRequests { get; set; }
    public decimal?  GoldPrice21K       { get; set; }
    public DateTime? GoldPriceFetchedAt { get; set; }
    public DateOnly? GoldPriceDate      { get; set; }
    public string?   GoldPriceSource    { get; set; }
}
