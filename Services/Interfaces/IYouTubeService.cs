namespace HalalStocks.Services.Interfaces;

public record YouTubeOEmbedResult(bool Ok, string? Title, string? ThumbnailUrl);

public interface IYouTubeService
{
    bool TryExtractVideoId(string url, out string videoId);
    Task<YouTubeOEmbedResult> FetchOEmbedAsync(string videoId);
}
