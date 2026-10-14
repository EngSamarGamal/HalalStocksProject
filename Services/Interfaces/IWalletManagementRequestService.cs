using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

public interface IWalletManagementRequestService
{
    Task<WalletManagementRequest?> GetByUserIdAsync(string userId);
    Task UpsertPendingRequestAsync(string userId);
    Task<IEnumerable<WalletManagementRequest>> GetPendingAsync();
    Task<IEnumerable<WalletManagementRequest>> GetApprovedAsync();
    Task<int> GetApprovedCountAsync();
    Task<bool> IsApprovedForUserAsync(string userId);
    Task<bool> ApproveAsync(int id);
    Task<bool> RejectAsync(int id);
    Task<bool> DeleteAsync(int id);
}
