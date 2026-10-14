using HalalStocks.Models;

namespace HalalStocks.Services.Interfaces;

public interface ICalculatorInstructionService
{
    /// <summary>
    /// The instructions HTML for a calculator. No saved row falls back to the built-in default;
    /// a saved-but-blank row returns null (the admin deliberately cleared it).
    /// </summary>
    Task<string?> GetContentAsync(CalculatorType type);

    /// <summary>Upserts the instructions; blank content (including an empty editor) is stored as null.</summary>
    Task SaveContentAsync(CalculatorType type, string? contentHtml);
}
