using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

public interface IRecommendationService
{
    Task<IEnumerable<StockRecommendation>> GetAllAsync();
    Task<IEnumerable<string>> GetDistinctPurityGradeLabelsAsync();
    Task<StockRecommendation?> GetByIdAsync(int id);
    Task<bool> ExistsForStockAsync(int stockId, int? excludeId = null);
    Task<StockRecommendation> CreateAsync(StockRecommendation recommendation);
    Task<StockRecommendation?> UpdateAsync(StockRecommendation recommendation);
    Task<bool> DeleteAsync(int id);
    Task<List<RecommendationType>> GetRecommendationTypesAsync();
    Task<RecommendationType?> GetRecommendationTypeByIdAsync(int id);
    Task<RecommendationType> CreateRecommendationTypeAsync(RecommendationType recommendationType);
    Task<bool> DeleteRecommendationTypeAsync(int id);
    Task<bool> RecommendationTypeInUseAsync(int id);
}
