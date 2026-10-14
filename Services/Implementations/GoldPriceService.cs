using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace HalalStocks.Services.Implementations;

public class GoldPriceService : IGoldPriceService
{
    private readonly IHttpClientFactory       _httpFactory;
    private readonly IMemoryCache             _cache;
    private readonly AppDbContext             _db;
    private readonly ILogger<GoldPriceService> _logger;
    private readonly string                   _apiKey;
    private readonly string                   _baseUrl;

    private const string PriceCacheKey  = "gold_21k_price";
    private const string TimeCacheKey   = "gold_21k_time";
    private const int    CacheTtlHours  = 1;

    public GoldPriceService(IHttpClientFactory httpFactory, IMemoryCache cache,
                            AppDbContext db, IConfiguration config,
                            ILogger<GoldPriceService> logger)
    {
        _httpFactory = httpFactory;
        _cache       = cache;
        _db          = db;
        _logger      = logger;
        _apiKey      = config["GoldApi:ApiKey"]  ?? "";
        _baseUrl     = config["GoldApi:BaseUrl"] ?? "https://www.goldapi.io/api";

        if (string.IsNullOrWhiteSpace(_apiKey))
            _logger.LogWarning("GoldPriceService: GoldApi:ApiKey is not configured in appsettings.json");
    }

    // ── Public interface ──────────────────────────────────────────────────────

    public async Task<decimal> GetGold21KGramPriceInEGPAsync()
    {
        // 1. Cache
        if (_cache.TryGetValue(PriceCacheKey, out decimal cached))
            return cached;

        // 2. DB — most recent entry regardless of date
        var snapshot = await LatestFromDbAsync();
        if (snapshot != null)
        {
            StoreInCache(snapshot.PriceGram21K, snapshot.FetchedAt);
            return snapshot.PriceGram21K;
        }

        // 3. API fallback (DB is empty)
        var price = await CallApiAsync();
        if (price > 0)
        {
            await PersistApiPriceAsync(price);
            StoreInCache(price, DateTime.UtcNow);
        }
        return price;
    }

    public async Task<GoldPriceSnapshot?> GetSnapshotAsync()
    {
        if (_cache.TryGetValue(PriceCacheKey, out decimal price) &&
            _cache.TryGetValue(TimeCacheKey,  out DateTime time))
        {
            // Return a lightweight object — Source/PriceDate may not be in cache,
            // so fetch the real row for full details.
            var row = await LatestFromDbAsync();
            return row ?? new GoldPriceSnapshot { PriceGram21K = price, FetchedAt = time };
        }

        var snapshot = await LatestFromDbAsync();
        if (snapshot != null)
            StoreInCache(snapshot.PriceGram21K, snapshot.FetchedAt);
        return snapshot;
    }

    public async Task<decimal> ForceRefreshAsync()
    {
        var price = await CallApiAsync();
        if (price > 0)
        {
            await PersistApiPriceAsync(price);
            StoreInCache(price, DateTime.UtcNow);
        }
        return price;
    }

    public async Task<GoldPriceSnapshot?> GetByDateAsync(DateOnly date)
        => await _db.GoldPriceSnapshots.FirstOrDefaultAsync(s => s.PriceDate == date);

    // ── Admin management ──────────────────────────────────────────────────────

    public async Task<List<GoldPriceSnapshot>> GetAllSnapshotsAsync()
        => await _db.GoldPriceSnapshots
                    .OrderByDescending(s => s.PriceDate)
                    .ToListAsync();

    public async Task SaveManualPriceAsync(DateOnly date, decimal price)
    {
        var existing = await _db.GoldPriceSnapshots
                                .FirstOrDefaultAsync(s => s.PriceDate == date);
        if (existing != null)
        {
            existing.PriceGram21K = price;
            existing.Source       = "Manual";
            existing.FetchedAt    = DateTime.UtcNow;
        }
        else
        {
            _db.GoldPriceSnapshots.Add(new GoldPriceSnapshot
            {
                PriceDate    = date,
                PriceGram21K = price,
                Source       = "Manual",
                FetchedAt    = DateTime.UtcNow
            });
        }
        await _db.SaveChangesAsync();
        InvalidateCache();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var snapshot = await _db.GoldPriceSnapshots.FindAsync(id);
        if (snapshot == null) return false;
        _db.GoldPriceSnapshots.Remove(snapshot);
        await _db.SaveChangesAsync();
        InvalidateCache();
        return true;
    }

    public void InvalidateCache()
    {
        _cache.Remove(PriceCacheKey);
        _cache.Remove(TimeCacheKey);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Task<GoldPriceSnapshot?> LatestFromDbAsync()
        => _db.GoldPriceSnapshots
              .OrderByDescending(s => s.PriceDate)
              .FirstOrDefaultAsync();

    private async Task<decimal> CallApiAsync()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogError("GoldPriceService: cannot call API — ApiKey is empty");
            return -1m;
        }

        try
        {
            var http    = _httpFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/XAU/EGP");
            request.Headers.Add("x-access-token", _apiKey);
            var response = await http.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("GoldPriceService: API error {Status} — {Body}",
                    (int)response.StatusCode, body);
                return -1m;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("price_gram_21k").GetDecimal();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GoldPriceService: exception calling API");
            return -1m;
        }
    }

    private async Task PersistApiPriceAsync(decimal price)
    {
        try
        {
            var today    = DateOnly.FromDateTime(DateTime.UtcNow);
            var existing = await _db.GoldPriceSnapshots
                                    .FirstOrDefaultAsync(s => s.PriceDate == today);
            if (existing != null)
            {
                // Only overwrite if it was also an API entry — never clobber a manual price
                if (existing.Source == "Api")
                {
                    existing.PriceGram21K = price;
                    existing.FetchedAt    = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }
            }
            else
            {
                _db.GoldPriceSnapshots.Add(new GoldPriceSnapshot
                {
                    PriceDate    = today,
                    PriceGram21K = price,
                    Source       = "Api",
                    FetchedAt    = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GoldPriceService: failed to persist API price to DB");
        }
    }

    private void StoreInCache(decimal price, DateTime fetchedAt)
    {
        var opts = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromHours(CacheTtlHours));
        _cache.Set(PriceCacheKey, price,     opts);
        _cache.Set(TimeCacheKey,  fetchedAt, opts);
    }
}
