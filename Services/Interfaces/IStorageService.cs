namespace HalalStocks.Services.Interfaces;

public interface IStorageService
{
    Task<string?> UploadAsync(IFormFile file, string folder,
        string[]? allowedExtensions = null, long maxBytes = 30 * 1024 * 1024);
    Task DeleteAsync(string? fileUrlOrKey);
}
