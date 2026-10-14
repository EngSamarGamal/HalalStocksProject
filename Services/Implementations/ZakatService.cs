using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Services.Implementations;

public class ZakatService : IZakatService
{
    private readonly AppDbContext _db;
    public ZakatService(AppDbContext db) => _db = db;

    public async Task<IEnumerable<ZakatRecord>> GetUserRecordsAsync(string userId) =>
        await _db.ZakatRecords
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.EntryDate)
            .ToListAsync();

    public async Task<ZakatRecord?> GetRecordAsync(int id, string userId) =>
        await _db.ZakatRecords.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

    public async Task<ZakatRecord> SaveRecordAsync(ZakatRecord record)
    {
        _db.ZakatRecords.Add(record);
        await _db.SaveChangesAsync();
        return record;
    }

    public async Task<bool> UpdateRecordAsync(int id, string userId, ZakatRecord updated)
    {
        var record = await _db.ZakatRecords.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (record == null) return false;

        record.CashBalance       = updated.CashBalance;
        record.StocksMarketValue = updated.StocksMarketValue;
        record.TotalPortfolio    = updated.TotalPortfolio;
        record.GoldGramPrice21K  = updated.GoldGramPrice21K;
        record.NisabValue        = updated.NisabValue;
        record.ReachedNisab      = updated.ReachedNisab;
        record.ZakatAmount       = updated.ZakatAmount;
        record.Status            = updated.Status;
        record.EntryDate         = updated.EntryDate;
        record.HijriDate         = updated.HijriDate;
        record.Notes             = updated.Notes;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteRecordAsync(int id, string userId)
    {
        var record = await _db.ZakatRecords.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
        if (record == null) return false;
        _db.ZakatRecords.Remove(record);
        await _db.SaveChangesAsync();
        return true;
    }
}
