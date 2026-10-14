using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Services.Implementations;

public class RecommendationService : IRecommendationService
{
    private readonly AppDbContext _db;
    public RecommendationService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<StockRecommendation>> GetAllAsync() =>
        await _db.StockRecommendations
            .Include(r => r.Stock)
            .Include(r => r.RecommendationType)
            .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync();

    public async Task<IEnumerable<string>> GetDistinctPurityGradeLabelsAsync() =>
        await _db.StockRecommendations
            .Select(r => r.PurityGradeLabel)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Distinct()
            .OrderBy(label => label)
            .ToListAsync();

    public async Task<StockRecommendation?> GetByIdAsync(int id) =>
        await _db.StockRecommendations
            .Include(r => r.Stock)
            .Include(r => r.RecommendationType)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<bool> ExistsForStockAsync(int stockId, int? excludeId = null)
    {
        var query = _db.StockRecommendations.Where(r => r.StockId == stockId);
        if (excludeId.HasValue)
            query = query.Where(r => r.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<StockRecommendation> CreateAsync(StockRecommendation recommendation)
    {
        recommendation.CreatedAt = DateTime.UtcNow;
        recommendation.UpdatedAt = DateTime.UtcNow;
        _db.StockRecommendations.Add(recommendation);
        await _db.SaveChangesAsync();
        return recommendation;
    }

    public async Task<StockRecommendation?> UpdateAsync(StockRecommendation recommendation)
    {
        var existing = await _db.StockRecommendations.FindAsync(recommendation.Id);
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
        existing.IsHidden          = recommendation.IsHidden;
        existing.UpdatedAt         = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var recommendation = await _db.StockRecommendations.FindAsync(id);
        if (recommendation == null) return false;

        _db.StockRecommendations.Remove(recommendation);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<List<RecommendationType>> GetRecommendationTypesAsync() =>
        await _db.RecommendationTypes
            .OrderBy(t => t.Id)
            .ToListAsync();

    public async Task<RecommendationType?> GetRecommendationTypeByIdAsync(int id) =>
        await _db.RecommendationTypes.FindAsync(id);

    public async Task<RecommendationType> CreateRecommendationTypeAsync(RecommendationType recommendationType)
    {
        _db.RecommendationTypes.Add(recommendationType);
        await _db.SaveChangesAsync();
        return recommendationType;
    }

    public async Task<bool> DeleteRecommendationTypeAsync(int id)
    {
        var type = await _db.RecommendationTypes.FindAsync(id);
        if (type == null) return false;

        _db.RecommendationTypes.Remove(type);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RecommendationTypeInUseAsync(int id) =>
        await _db.StockRecommendations.AnyAsync(r => r.RecommendationTypeId == id) ||
        await _db.UserStockRecommendations.AnyAsync(r => r.RecommendationTypeId == id);
}
