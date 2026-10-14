namespace HalalStocks.Services.Interfaces;

public interface ITradingViewPriceService
{
    /// <summary>
    /// Current market price per ticker, batched into a single outbound request.
    /// Tickers with no price available are simply absent from the result — the call never throws.
    /// </summary>
    Task<IReadOnlyDictionary<string, decimal>> GetPricesAsync(IEnumerable<string> tickers);

    /// <summary>Current market price for one ticker, or null when unavailable.</summary>
    Task<decimal?> GetPriceAsync(string ticker);
}
