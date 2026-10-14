using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Services.Implementations;

public class InvestmentWalletService : IInvestmentWalletService
{
    private readonly AppDbContext _db;

    public InvestmentWalletService(AppDbContext db) => _db = db;

    public async Task<InvestmentWallet?> GetByUserIdAsync(string userId) =>
        await _db.InvestmentWallets
            .Include(w => w.Items)
            .ThenInclude(i => i.Stock)
            .ThenInclude(s => s.Category)
            .FirstOrDefaultAsync(w => w.UserId == userId);

    public async Task<InvestmentWallet> CreateOrUpdateCashBalanceAsync(string userId, decimal cashBalance)
    {
        var wallet = await _db.InvestmentWallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null)
        {
            wallet = new InvestmentWallet { UserId = userId, CashBalance = cashBalance, UpdatedAt = DateTime.UtcNow };
            _db.InvestmentWallets.Add(wallet);
        }
        else
        {
            if (cashBalance < await _db.WalletItems.Where(i => i.WalletId == wallet.Id).SumAsync(i => i.TotalPurchase))
                throw new InvalidOperationException("لا يمكن تخفيض السيولة أقل من إجمالي الاستثمارات الحالية");

            wallet.CashBalance = cashBalance;
            wallet.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        var items = await _db.WalletItems.Where(i => i.WalletId == wallet.Id).ToListAsync();
        foreach (var item in items)
        {
            item.RelativeWeightPercent = wallet.CashBalance == 0 ? 0 : item.TotalPurchase / wallet.CashBalance * 100m;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return wallet;
    }

    public async Task<InvestmentWallet> SetMaxRiskPercentAsync(string userId, decimal riskPercent)
    {
        var wallet = await _db.InvestmentWallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null)
        {
            wallet = new InvestmentWallet { UserId = userId, CashBalance = 0, MaxRiskPercent = riskPercent, UpdatedAt = DateTime.UtcNow };
            _db.InvestmentWallets.Add(wallet);
        }
        else
        {
            wallet.MaxRiskPercent = riskPercent;
            wallet.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return wallet;
    }

    public async Task<WalletItem?> GetItemByIdAsync(int id) =>
        await _db.WalletItems
            .Include(i => i.Stock)
            .FirstOrDefaultAsync(i => i.Id == id);

    public async Task<List<WalletItem>> GetItemsByWalletIdAsync(int walletId) =>
        await _db.WalletItems
            .Where(i => i.WalletId == walletId)
            .Include(i => i.Stock)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync();

    public async Task<WalletItem> CreateItemAsync(WalletItem item)
    {
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        _db.WalletItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<WalletItem?> UpdateItemAsync(WalletItem item)
    {
        var existing = await _db.WalletItems.FindAsync(item.Id);
        if (existing == null) return null;

        existing.StockId = item.StockId;
        existing.Quantity = item.Quantity;
        existing.PurchasePrice = item.PurchasePrice;
        existing.TotalPurchase = item.TotalPurchase;
        existing.RelativeWeightPercent = item.RelativeWeightPercent;
        existing.TargetPrice = item.TargetPrice;
        existing.StopLossPrice = item.StopLossPrice;
        existing.ExpectedProfit = item.ExpectedProfit;
        existing.ExpectedLoss = item.ExpectedLoss;
        existing.TargetProfitPercent = item.TargetProfitPercent;
        existing.ExpectedLossPercent = item.ExpectedLossPercent;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteItemAsync(int id)
    {
        var item = await _db.WalletItems.FindAsync(id);
        if (item == null) return false;

        _db.WalletItems.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<(decimal TotalInvested, decimal CurrentLiquidity, decimal TotalExpectedProfit, decimal TotalExpectedLoss, decimal TotalRelativeWeight)> GetSummaryAsync(string userId)
    {
        var wallet = await _db.InvestmentWallets.FirstOrDefaultAsync(w => w.UserId == userId);
        if (wallet == null)
            return (0, 0, 0, 0, 0);

        var items = await _db.WalletItems.Where(i => i.WalletId == wallet.Id).ToListAsync();
        var totalInvested = items.Sum(i => i.TotalPurchase);
        var totalExpectedProfit = items.Sum(i => i.ExpectedProfit);
        var totalExpectedLoss = items.Sum(i => i.ExpectedLoss);
        var totalRelativeWeight = items.Sum(i => i.RelativeWeightPercent);
        return (totalInvested, wallet.CashBalance - totalInvested, totalExpectedProfit, totalExpectedLoss, totalRelativeWeight);
    }
}
