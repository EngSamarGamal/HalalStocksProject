using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Services.Implementations;

public class DividendPurificationService : IDividendPurificationService
{
    private readonly AppDbContext _db;
    public DividendPurificationService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<DividendPurificationRecord>> GetUserRecordsAsync(string userId) =>
        await _db.DividendPurificationRecords
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<DividendPurificationRecord> SaveRecordAsync(DividendPurificationRecord record)
    {
        _db.DividendPurificationRecords.Add(record);
        await _db.SaveChangesAsync();
        return record;
    }

    public async Task<bool> DeleteRecordAsync(int id, string userId)
    {
        var record = await _db.DividendPurificationRecords.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (record == null) return false;
        _db.DividendPurificationRecords.Remove(record);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TogglePaidAsync(int id, string userId)
    {
        var record = await _db.DividendPurificationRecords.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (record == null) return false;
        record.IsPaid = !record.IsPaid;
        await _db.SaveChangesAsync();
        return true;
    }
}
