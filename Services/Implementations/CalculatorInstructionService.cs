using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace HalalStocks.Services.Implementations;

public class CalculatorInstructionService : ICalculatorInstructionService
{
    private readonly AppDbContext _db;
    public CalculatorInstructionService(AppDbContext db) => _db = db;

    public async Task<string?> GetContentAsync(CalculatorType type)
    {
        var row = await _db.CalculatorInstructions.AsNoTracking().FirstOrDefaultAsync(i => i.Type == type);
        return row == null ? CalculatorInstructionDefaults.For(type) : Normalize(row.ContentHtml);
    }

    public async Task SaveContentAsync(CalculatorType type, string? contentHtml)
    {
        var row = await _db.CalculatorInstructions.FirstOrDefaultAsync(i => i.Type == type);
        if (row == null)
        {
            row = new CalculatorInstruction { Type = type };
            _db.CalculatorInstructions.Add(row);
        }
        row.ContentHtml = Normalize(contentHtml);
        await _db.SaveChangesAsync();
    }

    // An untouched Quill editor reports "<p><br></p>", which has no visible text — treat it as empty.
    private static string? Normalize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        var visibleText = Regex.Replace(html, "<[^>]*>|&nbsp;", "").Trim();
        return visibleText.Length == 0 ? null : html.Trim();
    }
}
