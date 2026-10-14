using HalalStocks.Models;

namespace HalalStocks.ViewModels;

public class RecommendationTableViewModel
{
    public string SectionKey { get; set; } = "general";
    public List<string> PurityGradeLabels { get; set; } = new();
    public List<RecommendationBase> Recommendations { get; set; } = new();
    public bool CanManage { get; set; }
    public string EditUrlPrefix { get; set; } = "/recommendations";
    public string DeleteUrlPrefix { get; set; } = "/recommendations";
}
