using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Services.Implementations;

public class RecommendationRequestService : IRecommendationRequestService
{
    private readonly AppDbContext _db;
    public RecommendationRequestService(AppDbContext db) => _db = db;

    public async Task<RecommendationRequest?> GetByUserIdAsync(string userId) =>
        await _db.RecommendationRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.UserId == userId);

    public async Task UpsertPendingRequestAsync(string userId)
    {
        var existing = await _db.RecommendationRequests
            .FirstOrDefaultAsync(r => r.UserId == userId);

        if (existing == null)
        {
            _db.RecommendationRequests.Add(new RecommendationRequest
            {
                UserId      = userId,
                Status      = RecommendationRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Status     = RecommendationRequestStatus.Pending;
            existing.RequestedAt = DateTime.UtcNow;
            existing.DecidedAt  = null;
            existing.ApprovedAt = null;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<RecommendationRequest>> GetPendingAsync() =>
        await _db.RecommendationRequests
            .Include(r => r.User)
            .Where(r => r.Status == RecommendationRequestStatus.Pending)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

    public async Task<IEnumerable<RecommendationRequest>> GetApprovedAsync() =>
        await _db.RecommendationRequests
            .Include(r => r.User)
            .Where(r => r.Status == RecommendationRequestStatus.Approved)
            .OrderByDescending(r => r.ApprovedAt)
            .ToListAsync();

    public async Task<int> GetApprovedCountAsync() =>
        await _db.RecommendationRequests
            .CountAsync(r => r.Status == RecommendationRequestStatus.Approved);

    public async Task<bool> ApproveAsync(int id)
    {
        var request = await _db.RecommendationRequests.FindAsync(id);
        if (request == null) return false;

        var now = DateTime.UtcNow;
        request.Status     = RecommendationRequestStatus.Approved;
        request.ApprovedAt = now;
        request.DecidedAt  = now;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectAsync(int id)
    {
        var request = await _db.RecommendationRequests.FindAsync(id);
        if (request == null) return false;

        request.Status     = RecommendationRequestStatus.Rejected;
        request.DecidedAt  = DateTime.UtcNow;
        request.ApprovedAt = null;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var request = await _db.RecommendationRequests.FindAsync(id);
        if (request == null) return false;

        _db.RecommendationRequests.Remove(request);
        await _db.SaveChangesAsync();
        return true;
    }
}
