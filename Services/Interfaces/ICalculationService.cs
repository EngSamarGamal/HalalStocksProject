using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

public interface ICalculationService
{
    Task<CalculationSession> SaveAsync(string userId, string? title, IEnumerable<CalculationEntry> entries);
    Task<IEnumerable<CalculationSession>> GetUserSessionsAsync(string userId);
    Task<CalculationSession?> GetSessionAsync(int id, string userId);
    Task<bool> DeleteAsync(int id, string userId);
    Task<bool> TogglePaidAsync(int id, string userId);
    Task<bool> UpdateAsync(int id, string userId, string? title, IEnumerable<CalculationEntry> entries);
}
