using HalalStocks.Models;

namespace HalalStocks.ViewModels;

public enum RecommendationsPageState
{
    Gate,
    Pending,
    List
}

public class RecommendationsIndexViewModel
{
    public RecommendationsPageState PageState { get; set; }
    public RecommendationRequest? UserRequest { get; set; }
    public List<StockRecommendation> Recommendations { get; set; } = new();
    public List<UserStockRecommendation> PrivateRecommendations { get; set; } = new();
    public List<string> PurityGradeLabels { get; set; } = new();
    public List<string> PrivatePurityGradeLabels { get; set; } = new();
    public bool IsAdmin { get; set; }
}
