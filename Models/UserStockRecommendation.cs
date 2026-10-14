namespace HalalStocks.Models;

public class UserStockRecommendation : RecommendationBase
{
    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;
}
