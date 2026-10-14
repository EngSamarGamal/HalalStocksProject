using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

public interface IInvestmentWalletService
{
    Task<InvestmentWallet?> GetByUserIdAsync(string userId);
    Task<InvestmentWallet> CreateOrUpdateCashBalanceAsync(string userId, decimal cashBalance);
    Task<InvestmentWallet> SetMaxRiskPercentAsync(string userId, decimal riskPercent);
    Task<WalletItem?> GetItemByIdAsync(int id);
    Task<List<WalletItem>> GetItemsByWalletIdAsync(int walletId);
    Task<WalletItem> CreateItemAsync(WalletItem item);
    Task<WalletItem?> UpdateItemAsync(WalletItem item);
    Task<bool> DeleteItemAsync(int id);
    Task<(decimal TotalInvested, decimal CurrentLiquidity, decimal TotalExpectedProfit, decimal TotalExpectedLoss, decimal TotalRelativeWeight)> GetSummaryAsync(string userId);
}
