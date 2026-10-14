using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Services.Implementations;

public class CalculationService : ICalculationService
{
    private readonly AppDbContext _db;
    public CalculationService(AppDbContext db) => _db = db;

    public async Task<CalculationSession> SaveAsync(string userId, string? title, IEnumerable<CalculationEntry> entries)
    {
        var list    = entries.ToList();
        var session = new CalculationSession
        {
            UserId            = userId,
            Title             = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
            TotalProfit       = list.Sum(e => e.Profit),
            TotalPurification = list.Sum(e => e.Purification),
            CreatedAt         = DateTime.UtcNow,
            Entries           = list
        };
        _db.CalculationSessions.Add(session);
        await _db.SaveChangesAsync();
        return session;
    }

    public async Task<IEnumerable<CalculationSession>> GetUserSessionsAsync(string userId) =>
        await _db.CalculationSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

    public async Task<CalculationSession?> GetSessionAsync(int id, string userId) =>
        await _db.CalculationSessions
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var session = await _db.CalculationSessions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        if (session == null) return false;
        _db.CalculationSessions.Remove(session);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateAsync(int id, string userId, string? title, IEnumerable<CalculationEntry> entries)
    {
        var session = await _db.CalculationSessions
            .Include(s => s.Entries)
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        if (session == null) return false;

        session.Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();

        _db.CalculationEntries.RemoveRange(session.Entries);

        var list = entries.ToList();
        foreach (var e in list) { e.Id = 0; e.SessionId = id; }

        session.TotalProfit       = list.Sum(e => e.Profit);
        session.TotalPurification = list.Sum(e => e.Purification);
        session.Entries           = list;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TogglePaidAsync(int id, string userId)
    {
        var session = await _db.CalculationSessions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        if (session == null) return false;
        session.IsPaid = !session.IsPaid;
        await _db.SaveChangesAsync();
        return true;
    }
}
