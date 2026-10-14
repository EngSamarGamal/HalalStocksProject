using HalalStocks.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Text;
using System.Text.Json;

namespace HalalStocks.Services.Implementations;

/// <summary>
/// Current market prices from TradingView's scanner endpoint.
/// The endpoint is keyless but unofficial, so the URL/exchange live in configuration and every
/// failure is logged and swallowed — callers fall back to the last price they persisted.
/// Follows the GoldPriceService shape: IHttpClientFactory + IMemoryCache + System.Text.Json.
/// </summary>
public class TradingViewPriceService : ITradingViewPriceService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TradingViewPriceService> _logger;
    private readonly string _scannerUrl;
    private readonly string _exchangePrefix;
    private readonly int _cacheMinutes;

    private const string CacheKeyPrefix = "tv_price_";

    public TradingViewPriceService(IHttpClientFactory httpFactory, IMemoryCache cache,
                                   IConfiguration config, ILogger<TradingViewPriceService> logger)
    {
        _httpFactory    = httpFactory;
        _cache          = cache;
        _logger         = logger;
        _scannerUrl     = config["TradingView:ScannerUrl"]     ?? "https://scanner.tradingview.com/egypt/scan";
        _exchangePrefix = config["TradingView:ExchangePrefix"] ?? "EGX";
        _cacheMinutes   = int.TryParse(config["TradingView:CacheMinutes"], out var minutes) && minutes > 0
            ? minutes
            : 15;
    }

    public async Task<decimal?> GetPriceAsync(string ticker)
    {
        if (string.IsNullOrWhiteSpace(ticker)) return null;
        var prices = await GetPricesAsync(new[] { ticker });
        return prices.TryGetValue(Normalize(ticker), out var price) ? price : null;
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetPricesAsync(IEnumerable<string> tickers)
    {
        var result = new Dictionary<string, decimal>();

        var wanted = tickers
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(Normalize)
            .Distinct()
            .ToList();

        if (wanted.Count == 0) return result;

        var missing = new List<string>();
        foreach (var ticker in wanted)
        {
            if (_cache.TryGetValue(CacheKeyPrefix + ticker, out decimal cached))
                result[ticker] = cached;
            else
                missing.Add(ticker);
        }

        if (missing.Count == 0) return result;

        var fetched = await FetchAsync(missing);
        foreach (var pair in fetched)
        {
            _cache.Set(CacheKeyPrefix + pair.Key, pair.Value,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(_cacheMinutes)));
            result[pair.Key] = pair.Value;
        }

        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string Normalize(string ticker) => ticker.Trim().ToUpperInvariant();

    private async Task<Dictionary<string, decimal>> FetchAsync(List<string> tickers)
    {
        var prices = new Dictionary<string, decimal>();

        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                symbols = new
                {
                    tickers = tickers.Select(t => $"{_exchangePrefix}:{t}").ToArray(),
                    query   = new { types = Array.Empty<string>() }
                },
                columns = new[] { "close" }
            });

            var http     = _httpFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(10);
            var content  = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await http.PostAsync(_scannerUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("TradingViewPriceService: scanner returned {Status}", (int)response.StatusCode);
                return prices;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
            {
                _logger.LogError("TradingViewPriceService: unexpected scanner response shape");
                return prices;
            }

            foreach (var row in data.EnumerateArray())
            {
                if (!row.TryGetProperty("s", out var symbolElement)) continue;
                if (!row.TryGetProperty("d", out var values) || values.ValueKind != JsonValueKind.Array) continue;

                var close = values.EnumerateArray().FirstOrDefault();
                if (close.ValueKind != JsonValueKind.Number) continue;

                // "EGX:COMI" → "COMI"
                var symbol = symbolElement.GetString() ?? string.Empty;
                var ticker = symbol.Contains(':') ? symbol[(symbol.IndexOf(':') + 1)..] : symbol;
                if (string.IsNullOrWhiteSpace(ticker)) continue;

                prices[Normalize(ticker)] = close.GetDecimal();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TradingViewPriceService: exception fetching prices from the scanner");
        }

        return prices;
    }
}
