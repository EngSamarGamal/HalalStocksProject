using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

public interface ICapitalPurificationService
{
    Task<IEnumerable<CapitalPurificationRecord>> GetUserRecordsAsync(string userId);
    Task<CapitalPurificationRecord?> GetRecordAsync(int id, string userId);
    Task<CapitalPurificationRecord> SaveRecordAsync(CapitalPurificationRecord record);
    Task<bool> UpdateRecordAsync(int id, string userId, CapitalPurificationRecord updated);
    Task<bool> DeleteRecordAsync(int id, string userId);
    Task<bool> TogglePaidAsync(int id, string userId);
}
