using HalalStocks.Services.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HalalStocks.Services.Implementations;

public class YouTubeService : IYouTubeService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<YouTubeService> _logger;

    private static readonly Regex[] VideoIdPatterns =
    {
        new(@"youtu\.be/([A-Za-z0-9_-]{11})", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"youtube\.com/watch\?.*v=([A-Za-z0-9_-]{11})", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"youtube\.com/embed/([A-Za-z0-9_-]{11})", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"youtube\.com/shorts/([A-Za-z0-9_-]{11})", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    };

    public YouTubeService(IHttpClientFactory httpFactory, ILogger<YouTubeService> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public bool TryExtractVideoId(string url, out string videoId)
    {
        videoId = string.Empty;
        if (string.IsNullOrWhiteSpace(url)) return false;

        foreach (var pattern in VideoIdPatterns)
        {
            var match = pattern.Match(url);
            if (match.Success)
            {
                videoId = match.Groups[1].Value;
                return true;
            }
        }
        return false;
    }

    public async Task<YouTubeOEmbedResult> FetchOEmbedAsync(string videoId)
    {
        try
        {
            var watchUrl = $"https://www.youtube.com/watch?v={videoId}";
            var oembedUrl = $"https://www.youtube.com/oembed?url={Uri.EscapeDataString(watchUrl)}&format=json";

            var http = _httpFactory.CreateClient();
            var response = await http.GetAsync(oembedUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("YouTubeService: oEmbed returned {Status} for {VideoId}",
                    (int)response.StatusCode, videoId);
                return new YouTubeOEmbedResult(false, null, null);
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            string? title = doc.RootElement.TryGetProperty("title", out var titleProp)
                ? titleProp.GetString() : null;
            string? thumbnailUrl = doc.RootElement.TryGetProperty("thumbnail_url", out var thumbProp)
                ? thumbProp.GetString() : null;

            return new YouTubeOEmbedResult(true, title, thumbnailUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "YouTubeService: exception fetching oEmbed for {VideoId}", videoId);
            return new YouTubeOEmbedResult(false, null, null);
        }
    }
}
