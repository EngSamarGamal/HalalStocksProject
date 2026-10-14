using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

public interface IDividendPurificationService
{
    Task<IEnumerable<DividendPurificationRecord>> GetUserRecordsAsync(string userId);
    Task<DividendPurificationRecord> SaveRecordAsync(DividendPurificationRecord record);
    Task<bool> DeleteRecordAsync(int id, string userId);
    Task<bool> TogglePaidAsync(int id, string userId);
}
