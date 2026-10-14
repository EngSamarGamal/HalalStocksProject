using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

public interface IRealPortfolioService
{
    Task<RealPortfolio?> GetByUserIdAsync(string userId);

    /// <summary>
    /// Sets رأس المال, then recomputes every lot's purchase weight against the new capital.
    /// Throws <see cref="InvalidOperationException"/> when the new capital is below the amount
    /// already invested.
    /// </summary>
    Task<RealPortfolio> CreateOrUpdateCapitalAsync(string userId, decimal capital);

    Task<RealPortfolioItem?> GetItemByIdAsync(int id);
    Task<RealPortfolioItem> CreateItemAsync(RealPortfolioItem item);
    Task<RealPortfolioItem?> UpdateItemAsync(RealPortfolioItem item);
    Task<bool> DeleteItemAsync(int id);

    /// <summary>Persists the latest market price fetched for a set of lots (fallback cache).</summary>
    Task SaveMarketPricesAsync(IReadOnlyDictionary<int, decimal> pricesByItemId);

    Task<(decimal Capital, decimal InvestedCapital, decimal AvailableCash)> GetSummaryAsync(string userId);
}
