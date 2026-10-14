using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

public interface IStockService
{
    Task<(IEnumerable<Stock> Items, int TotalCount)> GetStocksAsync(string? search, List<int>? categoryIds, ShariahStatus? status, List<int>? indexIds, int page, int pageSize, bool includeInactive = false, bool filterPure = false, bool filterSemiPure = false);
    Task<Stock?> GetByTickerAsync(string ticker);
    Task<IEnumerable<StockCategory>> GetCategoriesAsync();
    Task<IEnumerable<string>> GetExchangesAsync();
    Task<IEnumerable<StockIndex>> GetIndicesAsync();
    Task<Stock> CreateAsync(Stock stock, List<int> selectedIndexIds);
    Task<Stock> UpdateAsync(Stock stock, List<int> selectedIndexIds);
    Task DeleteAsync(int id);
    Task<Stock?> GetByIdAsync(int id);
    Task<IEnumerable<Stock>> GetPurifiableStocksAsync();
    Task<IEnumerable<Stock>> GetAllActiveStocksAsync();
    Task<IEnumerable<Stock>> SearchActiveStocksAsync(string? query, int take = 20);
    Task<Dictionary<int, DateTime>> GetLatestReviewDatesAsync(IEnumerable<int> stockIds);
}
