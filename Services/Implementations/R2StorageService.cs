using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using HalalStocks.Models.Settings;
using HalalStocks.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace HalalStocks.Services.Implementations;

public class R2StorageService : IStorageService
{
    private readonly CloudflareR2Settings _settings;
    private readonly IAmazonS3 _s3;

    private static readonly string[] DefaultAllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    public R2StorageService(IOptions<CloudflareR2Settings> options)
    {
        _settings = options.Value;

        var credentials = new BasicAWSCredentials(_settings.AccessKeyId, _settings.SecretAccessKey);
        var config = new AmazonS3Config
        {
            ServiceURL     = $"https://{_settings.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,
        };

        _s3 = new AmazonS3Client(credentials, config);
    }

    public async Task<string?> UploadAsync(IFormFile file, string folder,
        string[]? allowedExtensions = null, long maxBytes = 2 * 1024 * 1024)
    {
        if (file == null || file.Length == 0) return null;

        var allowed = allowedExtensions ?? DefaultAllowedExtensions;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext) || file.Length > maxBytes) return null;

        var fileName = $"{Guid.NewGuid()}{ext}";
        var key      = $"{folder}/{fileName}";

        using var stream = file.OpenReadStream();
        var request = new PutObjectRequest
        {
            BucketName  = _settings.BucketName,
            Key         = key,
            InputStream = stream,
            ContentType = file.ContentType,
            DisablePayloadSigning = true,
        };

        await _s3.PutObjectAsync(request);

        return $"{_settings.PublicUrl.TrimEnd('/')}/{key}";
    }

    public async Task DeleteAsync(string? fileUrlOrKey)
    {
        if (string.IsNullOrEmpty(fileUrlOrKey)) return;

        var key = fileUrlOrKey.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? ExtractKeyFromUrl(fileUrlOrKey)
            : fileUrlOrKey;

        if (string.IsNullOrEmpty(key)) return;

        try
        {
            await _s3.DeleteObjectAsync(_settings.BucketName, key);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // file not found — ignore silently
        }
    }

    private string ExtractKeyFromUrl(string url)
    {
        var publicUrl = _settings.PublicUrl.TrimEnd('/');
        return url.StartsWith(publicUrl, StringComparison.OrdinalIgnoreCase)
            ? url[publicUrl.Length..].TrimStart('/')
            : string.Empty;
    }
}
