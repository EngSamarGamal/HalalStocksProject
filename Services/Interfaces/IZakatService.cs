using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

public interface IZakatService
{
    Task<IEnumerable<ZakatRecord>> GetUserRecordsAsync(string userId);
    Task<ZakatRecord?> GetRecordAsync(int id, string userId);
    Task<ZakatRecord> SaveRecordAsync(ZakatRecord record);
    Task<bool> UpdateRecordAsync(int id, string userId, ZakatRecord updated);
    Task<bool> DeleteRecordAsync(int id, string userId);
}
