namespace HalalStocks.Models;

public class FinancialStatementType
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public ICollection<StockFinancialReview> Reviews { get; set; } = new List<StockFinancialReview>();
}
