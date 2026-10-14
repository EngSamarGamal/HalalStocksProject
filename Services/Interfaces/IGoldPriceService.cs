using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

public interface IGoldPriceService
{
    /// <summary>Returns price from cache → DB → API fallback.</summary>
    Task<decimal> GetGold21KGramPriceInEGPAsync();

    /// <summary>Returns the most recent snapshot for display.</summary>
    Task<GoldPriceSnapshot?> GetSnapshotAsync();

    /// <summary>Forces an API call and saves result to DB. Used by the admin refresh button.</summary>
    Task<decimal> ForceRefreshAsync();

    /// <summary>Returns the snapshot for a specific date, or null if not in DB.</summary>
    Task<GoldPriceSnapshot?> GetByDateAsync(DateOnly date);

    // ── Admin management ─────────────────────────────────────────────────────
    Task<List<GoldPriceSnapshot>> GetAllSnapshotsAsync();
    Task SaveManualPriceAsync(DateOnly date, decimal price);
    Task<bool> DeleteAsync(int id);
    void InvalidateCache();
}
