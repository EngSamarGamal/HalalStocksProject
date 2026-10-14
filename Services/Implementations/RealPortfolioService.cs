using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Services.Implementations;

public class RealPortfolioService : IRealPortfolioService
{
    private readonly AppDbContext _db;

    public RealPortfolioService(AppDbContext db) => _db = db;

    public async Task<RealPortfolio?> GetByUserIdAsync(string userId) =>
        await _db.RealPortfolios
            .Include(p => p.Items.OrderBy(i => i.PurchaseDate).ThenBy(i => i.Id))
            .ThenInclude(i => i.Stock)
            .ThenInclude(s => s.Category)
            .FirstOrDefaultAsync(p => p.UserId == userId);

    public async Task<RealPortfolio> CreateOrUpdateCapitalAsync(string userId, decimal capital)
    {
        var portfolio = await _db.RealPortfolios.FirstOrDefaultAsync(p => p.UserId == userId);
        if (portfolio == null)
        {
            portfolio = new RealPortfolio { UserId = userId, Capital = capital, UpdatedAt = DateTime.UtcNow };
            _db.RealPortfolios.Add(portfolio);
            await _db.SaveChangesAsync();
            return portfolio;
        }

        var invested = await _db.RealPortfolioItems
            .Where(i => i.PortfolioId == portfolio.Id)
            .SumAsync(i => (decimal?)i.TotalPurchase) ?? 0m;

        if (capital < invested)
            throw new InvalidOperationException("لا يمكن تخفيض رأس المال أقل من إجمالي القيمة الشرائية الحالية");

        portfolio.Capital = capital;
        portfolio.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var items = await _db.RealPortfolioItems.Where(i => i.PortfolioId == portfolio.Id).ToListAsync();
        foreach (var item in items)
        {
            item.PurchaseWeightPercent = capital == 0 ? 0 : item.TotalPurchase / capital * 100m;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return portfolio;
    }

    public async Task<RealPortfolioItem?> GetItemByIdAsync(int id) =>
        await _db.RealPortfolioItems
            .Include(i => i.Stock)
            .ThenInclude(s => s.Category)
            .FirstOrDefaultAsync(i => i.Id == id);

    public async Task<RealPortfolioItem> CreateItemAsync(RealPortfolioItem item)
    {
        item.CreatedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        _db.RealPortfolioItems.Add(item);
        await _db.SaveChangesAsync();
        return item;
    }

    public async Task<RealPortfolioItem?> UpdateItemAsync(RealPortfolioItem item)
    {
        var existing = await _db.RealPortfolioItems.FindAsync(item.Id);
        if (existing == null) return null;

        existing.StockId = item.StockId;
        existing.PurchaseDate = item.PurchaseDate;
        existing.Quantity = item.Quantity;
        existing.PurchasePrice = item.PurchasePrice;
        existing.TotalPurchase = item.TotalPurchase;
        existing.PurchaseWeightPercent = item.PurchaseWeightPercent;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteItemAsync(int id)
    {
        var item = await _db.RealPortfolioItems.FindAsync(id);
        if (item == null) return false;

        _db.RealPortfolioItems.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task SaveMarketPricesAsync(IReadOnlyDictionary<int, decimal> pricesByItemId)
    {
        if (pricesByItemId.Count == 0) return;

        var ids = pricesByItemId.Keys.ToList();
        var items = await _db.RealPortfolioItems.Where(i => ids.Contains(i.Id)).ToListAsync();
        if (items.Count == 0) return;

        var fetchedAt = DateTime.UtcNow;
        foreach (var item in items)
        {
            if (!pricesByItemId.TryGetValue(item.Id, out var price)) continue;
            item.LastMarketPrice = price;
            item.LastMarketPriceAt = fetchedAt;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<(decimal Capital, decimal InvestedCapital, decimal AvailableCash)> GetSummaryAsync(string userId)
    {
        var portfolio = await _db.RealPortfolios.FirstOrDefaultAsync(p => p.UserId == userId);
        if (portfolio == null) return (0, 0, 0);

        var invested = await _db.RealPortfolioItems
            .Where(i => i.PortfolioId == portfolio.Id)
            .SumAsync(i => (decimal?)i.TotalPurchase) ?? 0m;

        return (portfolio.Capital, invested, portfolio.Capital - invested);
    }
}
