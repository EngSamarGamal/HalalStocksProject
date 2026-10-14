namespace HalalStocks.Models;

public class RecommendationType
{
    public int Id { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public bool IsExpiredMarker { get; set; }

    public ICollection<StockRecommendation> StockRecommendations { get; set; } = new List<StockRecommendation>();
}
