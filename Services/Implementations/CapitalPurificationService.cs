using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Services.Implementations;

public class CapitalPurificationService : ICapitalPurificationService
{
    private readonly AppDbContext _db;
    public CapitalPurificationService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<CapitalPurificationRecord>> GetUserRecordsAsync(string userId) =>
        await _db.CapitalPurificationRecords
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<CapitalPurificationRecord?> GetRecordAsync(int id, string userId) =>
        await _db.CapitalPurificationRecords.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

    public async Task<CapitalPurificationRecord> SaveRecordAsync(CapitalPurificationRecord record)
    {
        _db.CapitalPurificationRecords.Add(record);
        await _db.SaveChangesAsync();
        return record;
    }

    public async Task<bool> UpdateRecordAsync(int id, string userId, CapitalPurificationRecord updated)
    {
        var record = await _db.CapitalPurificationRecords.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (record == null) return false;

        record.StockId            = updated.StockId;
        record.Ticker             = updated.Ticker;
        record.NameAr             = updated.NameAr;
        record.OwnedShares        = updated.OwnedShares;
        record.HoldingDays        = updated.HoldingDays;
        record.TotalCompanyShares = updated.TotalCompanyShares;
        record.OwnershipPercent   = updated.OwnershipPercent;
        record.TotalCompanyAssets = updated.TotalCompanyAssets;
        record.CapitalShare       = updated.CapitalShare;
        record.RibaActivityRatio  = updated.RibaActivityRatio;
        record.PurificationAmount = updated.PurificationAmount;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteRecordAsync(int id, string userId)
    {
        var record = await _db.CapitalPurificationRecords.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (record == null) return false;
        _db.CapitalPurificationRecords.Remove(record);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> TogglePaidAsync(int id, string userId)
    {
        var record = await _db.CapitalPurificationRecords.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (record == null) return false;
        record.IsPaid = !record.IsPaid;
        await _db.SaveChangesAsync();
        return true;
    }
}
