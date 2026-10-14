using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Services.Implementations;

public class WalletManagementRequestService : IWalletManagementRequestService
{
    private readonly AppDbContext _db;
    public WalletManagementRequestService(AppDbContext db) => _db = db;

    public async Task<WalletManagementRequest?> GetByUserIdAsync(string userId) =>
        await _db.WalletManagementRequests
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.UserId == userId);

    public async Task UpsertPendingRequestAsync(string userId)
    {
        var existing = await _db.WalletManagementRequests
            .FirstOrDefaultAsync(r => r.UserId == userId);

        if (existing == null)
        {
            _db.WalletManagementRequests.Add(new WalletManagementRequest
            {
                UserId      = userId,
                Status      = WalletManagementRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.Status      = WalletManagementRequestStatus.Pending;
            existing.RequestedAt = DateTime.UtcNow;
            existing.DecidedAt   = null;
            existing.ApprovedAt  = null;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<WalletManagementRequest>> GetPendingAsync() =>
        await _db.WalletManagementRequests
            .Include(r => r.User)
            .Where(r => r.Status == WalletManagementRequestStatus.Pending)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync();

    public async Task<IEnumerable<WalletManagementRequest>> GetApprovedAsync() =>
        await _db.WalletManagementRequests
            .Include(r => r.User)
            .Where(r => r.Status == WalletManagementRequestStatus.Approved)
            .OrderByDescending(r => r.ApprovedAt)
            .ToListAsync();

    public async Task<int> GetApprovedCountAsync() =>
        await _db.WalletManagementRequests
            .CountAsync(r => r.Status == WalletManagementRequestStatus.Approved);

    public async Task<bool> IsApprovedForUserAsync(string userId) =>
        await _db.WalletManagementRequests
            .AnyAsync(r => r.UserId == userId && r.Status == WalletManagementRequestStatus.Approved);

    public async Task<bool> ApproveAsync(int id)
    {
        var request = await _db.WalletManagementRequests.FindAsync(id);
        if (request == null) return false;

        var now = DateTime.UtcNow;
        request.Status     = WalletManagementRequestStatus.Approved;
        request.ApprovedAt = now;
        request.DecidedAt  = now;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RejectAsync(int id)
    {
        var request = await _db.WalletManagementRequests.FindAsync(id);
        if (request == null) return false;

        request.Status     = WalletManagementRequestStatus.Rejected;
        request.DecidedAt  = DateTime.UtcNow;
        request.ApprovedAt = null;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var request = await _db.WalletManagementRequests.FindAsync(id);
        if (request == null) return false;

        _db.WalletManagementRequests.Remove(request);
        await _db.SaveChangesAsync();
        return true;
    }
}
