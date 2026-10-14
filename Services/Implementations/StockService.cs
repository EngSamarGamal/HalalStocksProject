using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Services.Implementations;

public class StockService : IStockService
{
    private readonly AppDbContext _db;
    public StockService(AppDbContext db) => _db = db;

    private static readonly PurityGrade[] _gradeOrder =
    {
        PurityGrade.Pure,
        PurityGrade.SemiPure,
        PurityGrade.ShariaMixedA,
        PurityGrade.ShariaMixedB,
        PurityGrade.ShariaMixedC,
        PurityGrade.NonShariahProhibitedIncome,
        PurityGrade.NonShariahDebts,
        PurityGrade.NonShariahRibaInvestments,
        PurityGrade.NonShariahActivity,
    };

    private static int PurityPriority(PurityGrade g)
    {
        for (int i = 0; i < _gradeOrder.Length; i++)
            if (g.HasFlag(_gradeOrder[i])) return i;
        return _gradeOrder.Length; // None → last
    }

    public async Task<(IEnumerable<Stock> Items, int TotalCount)> GetStocksAsync(
        string? search, List<int>? categoryIds, ShariahStatus? status, List<int>? indexIds, int page, int pageSize,
        bool includeInactive = false, bool filterPure = false, bool filterSemiPure = false)
    {
        var query = _db.Stocks
            .Include(s => s.Category)
            .Include(s => s.Indices)
            .AsQueryable();

        if (!includeInactive)
            query = query.Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(x =>
                x.TickerSymbol.ToLower().Contains(s) ||
                x.NameAr.Contains(search) ||
                (x.NameEn != null && x.NameEn.ToLower().Contains(s)));
        }
        if (categoryIds != null && categoryIds.Count > 0) query = query.Where(x => categoryIds.Contains(x.CategoryId));
        if (status.HasValue) query = query.Where(x => x.ShariahStatus == status);
        if (indexIds != null && indexIds.Count > 0) query = query.Where(x => x.Indices.Any(i => indexIds.Contains(i.Id)));

        // Purity grade – OR between the two checkboxes, AND with every other filter
        if (filterPure && filterSemiPure)
            query = query.Where(x => (x.PurityGrades & (PurityGrade.Pure | PurityGrade.SemiPure)) != PurityGrade.None);
        else if (filterPure)
            query = query.Where(x => (x.PurityGrades & PurityGrade.Pure) == PurityGrade.Pure);
        else if (filterSemiPure)
            query = query.Where(x => (x.PurityGrades & PurityGrade.SemiPure) == PurityGrade.SemiPure);

        var total = await query.CountAsync();
        var all   = await query.ToListAsync();
        var items = all
            .OrderBy(x => PurityPriority(x.PurityGrades))
            .ThenBy(x => x.TickerSymbol)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        return (items, total);
    }

    public async Task<Stock?> GetByTickerAsync(string ticker) =>
        await _db.Stocks
            .Include(s => s.Category)
            .Include(s => s.Indices)
            .Include(s => s.FinancialReviews.OrderByDescending(r => r.ReviewDate))
                .ThenInclude(r => r.StatementType)
            .FirstOrDefaultAsync(s => s.TickerSymbol.ToLower() == ticker.ToLower() && s.IsActive);

    public async Task<IEnumerable<StockCategory>> GetCategoriesAsync() =>
        await _db.StockCategories.OrderBy(c => c.NameAr).ToListAsync();

    public async Task<IEnumerable<string>> GetExchangesAsync() =>
        await _db.Stocks.Where(s => s.IsActive).Select(s => s.Exchange).Distinct().ToListAsync();

    public async Task<IEnumerable<StockIndex>> GetIndicesAsync() =>
        await _db.StockIndices.OrderBy(i => i.NameAr).ToListAsync();

    public async Task<Stock> CreateAsync(Stock stock, List<int> selectedIndexIds)
    {
        stock.CreatedAt = DateTime.UtcNow;
        stock.UpdatedAt = DateTime.UtcNow;

        if (selectedIndexIds.Count > 0)
        {
            var indices = await _db.StockIndices
                .Where(i => selectedIndexIds.Contains(i.Id)).ToListAsync();
            foreach (var idx in indices) stock.Indices.Add(idx);
        }

        _db.Stocks.Add(stock);
        await _db.SaveChangesAsync();
        return stock;
    }

    public async Task<Stock> UpdateAsync(Stock stock, List<int> selectedIndexIds)
    {
        stock.UpdatedAt = DateTime.UtcNow;

        // Load existing indices for this stock so EF can track the collection
        var tracked = await _db.Stocks
            .Include(s => s.Indices)
            .FirstAsync(s => s.Id == stock.Id);

        // Copy scalar changes onto the tracked entity
        _db.Entry(tracked).CurrentValues.SetValues(stock);

        // Clear and re-add indices
        tracked.Indices.Clear();
        if (selectedIndexIds.Count > 0)
        {
            var indices = await _db.StockIndices
                .Where(i => selectedIndexIds.Contains(i.Id)).ToListAsync();
            foreach (var idx in indices) tracked.Indices.Add(idx);
        }

        await _db.SaveChangesAsync();
        return tracked;
    }

    public async Task<IEnumerable<Stock>> GetPurifiableStocksAsync() =>
        await _db.Stocks
            .Where(s => s.IsActive && s.ShariahStatus == ShariahStatus.Halal)
            .OrderBy(s => s.TickerSymbol)
            .ToListAsync();

    public async Task<IEnumerable<Stock>> GetAllActiveStocksAsync() =>
        await _db.Stocks
            .Where(s => s.IsActive)
            .OrderBy(s => s.TickerSymbol)
            .ToListAsync();

    public async Task<IEnumerable<Stock>> SearchActiveStocksAsync(string? query, int take = 20)
    {
        var stocks = _db.Stocks.Include(s => s.Category).Where(s => s.IsActive);

        var term = query?.Trim();
        if (!string.IsNullOrEmpty(term))
        {
            stocks = stocks.Where(s =>
                s.TickerSymbol.Contains(term) ||
                s.NameAr.Contains(term) ||
                (s.NameEn != null && s.NameEn.Contains(term)));
        }

        return await stocks
            .OrderBy(s => s.TickerSymbol)
            .Take(take)
            .ToListAsync();
    }

    // Latest financial-statements review date per stock (stocks with no review are simply absent).
    public async Task<Dictionary<int, DateTime>> GetLatestReviewDatesAsync(IEnumerable<int> stockIds)
    {
        var ids = stockIds.ToList();
        return await _db.StockFinancialReviews
            .Where(r => ids.Contains(r.StockId))
            .GroupBy(r => r.StockId)
            .Select(g => new { StockId = g.Key, Latest = g.Max(r => r.ReviewDate) })
            .ToDictionaryAsync(x => x.StockId, x => x.Latest);
    }

    public async Task DeleteAsync(int id)
    {
        var stock = await _db.Stocks.FindAsync(id);
        if (stock != null) { _db.Stocks.Remove(stock); await _db.SaveChangesAsync(); }
    }

    public async Task<Stock?> GetByIdAsync(int id) =>
        await _db.Stocks
            .Include(s => s.Category)
            .Include(s => s.Indices)
            .FirstOrDefaultAsync(s => s.Id == id);
}
