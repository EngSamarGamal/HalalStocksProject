using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Services.Implementations;

public class UserRecommendationService : IUserRecommendationService
{
    private readonly AppDbContext _db;
    public UserRecommendationService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<UserStockRecommendation>> GetByUserIdAsync(string userId) =>
        await _db.UserStockRecommendations
            .Where(r => r.UserId == userId)
            .Include(r => r.Stock)
            .Include(r => r.RecommendationType)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync();

    public async Task<UserStockRecommendation?> GetOwnedByIdAsync(int id, string userId) =>
        await _db.UserStockRecommendations
            .Include(r => r.Stock)
            .Include(r => r.RecommendationType)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

    public async Task<bool> ExistsForUserStockAsync(string userId, int stockId, int? excludeId = null)
    {
        var query = _db.UserStockRecommendations.Where(r => r.UserId == userId && r.StockId == stockId);
        if (excludeId.HasValue)
            query = query.Where(r => r.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<UserStockRecommendation> CreateAsync(UserStockRecommendation recommendation)
    {
        recommendation.CreatedAt = DateTime.UtcNow;
        recommendation.UpdatedAt = DateTime.UtcNow;
        _db.UserStockRecommendations.Add(recommendation);
        await _db.SaveChangesAsync();
        return recommendation;
    }

    public async Task<UserStockRecommendation?> UpdateAsync(UserStockRecommendation recommendation, string userId)
    {
        var existing = await _db.UserStockRecommendations
            .FirstOrDefaultAsync(r => r.Id == recommendation.Id && r.UserId == userId);
        if (existing == null) return null;

        existing.StockId           = recommendation.StockId;
        existing.PurityGradeLabel  = recommendation.PurityGradeLabel;
        existing.GeneralTrend      = recommendation.GeneralTrend;
        existing.ClosingPrice      = recommendation.ClosingPrice;
        existing.Support1          = recommendation.Support1;
        existing.Support2          = recommendation.Support2;
        existing.Resistance1       = recommendation.Resistance1;
        existing.Resistance2       = recommendation.Resistance2;
        existing.StopLoss          = recommendation.StopLoss;
        existing.YtdChangePercent  = recommendation.YtdChangePercent;
        existing.RecommendationTypeId = recommendation.RecommendationTypeId;
        existing.Notes             = recommendation.Notes;
        existing.UpdatedAt         = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var recommendation = await _db.UserStockRecommendations
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (recommendation == null) return false;

        _db.UserStockRecommendations.Remove(recommendation);
        await _db.SaveChangesAsync();
        return true;
    }
}
