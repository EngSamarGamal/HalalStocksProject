using HalalStocks.Services.Interfaces;

namespace HalalStocks.Services.Implementations;

public class LocalStorageService : IStorageService
{
    private readonly IWebHostEnvironment _env;

    private static readonly string[] DefaultAllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    public LocalStorageService(IWebHostEnvironment env) => _env = env;

    public async Task<string?> UploadAsync(IFormFile file, string folder,
        string[]? allowedExtensions = null, long maxBytes = 2 * 1024 * 1024)
    {
        if (file == null || file.Length == 0) return null;

        var allowed = allowedExtensions ?? DefaultAllowedExtensions;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext) || file.Length > maxBytes) return null;

        var dir = Path.Combine(_env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var fullPath = Path.Combine(dir, fileName);

        await using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/{folder}/{fileName}";
    }

    public Task DeleteAsync(string? fileUrlOrKey)
    {
        if (string.IsNullOrEmpty(fileUrlOrKey)) return Task.CompletedTask;

        // only handle local paths (starts with /)
        if (!fileUrlOrKey.StartsWith('/')) return Task.CompletedTask;

        var fullPath = Path.Combine(_env.WebRootPath, fileUrlOrKey.TrimStart('/'));
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }
}
