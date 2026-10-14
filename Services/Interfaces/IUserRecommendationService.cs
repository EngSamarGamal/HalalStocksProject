using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

public interface IUserRecommendationService
{
    Task<IEnumerable<UserStockRecommendation>> GetByUserIdAsync(string userId);
    Task<UserStockRecommendation?> GetOwnedByIdAsync(int id, string userId);
    Task<bool> ExistsForUserStockAsync(string userId, int stockId, int? excludeId = null);
    Task<UserStockRecommendation> CreateAsync(UserStockRecommendation recommendation);
    Task<UserStockRecommendation?> UpdateAsync(UserStockRecommendation recommendation, string userId);
    Task<bool> DeleteAsync(int id, string userId);
}
