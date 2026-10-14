using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

public interface IRecommendationRequestService
{
    Task<RecommendationRequest?> GetByUserIdAsync(string userId);
    Task UpsertPendingRequestAsync(string userId);
    Task<IEnumerable<RecommendationRequest>> GetPendingAsync();
    Task<IEnumerable<RecommendationRequest>> GetApprovedAsync();
    Task<int> GetApprovedCountAsync();
    Task<bool> ApproveAsync(int id);
    Task<bool> RejectAsync(int id);
    Task<bool> DeleteAsync(int id);
}
