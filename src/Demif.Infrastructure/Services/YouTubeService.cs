using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Demif.Application.Abstractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Demif.Infrastructure.Services;

/// <summary>
/// YouTube Data API v3 service — fetch video info + captions.
/// Sử dụng REST API trực tiếp (HttpClient) thay vì Google SDK nặng.
/// 
/// ✅ Free tier: 10,000 units/ngày (1 video.list = 1 unit, 1 captions.list = 50 units)
/// ✅ Không cần OAuth — chỉ API Key
/// </summary>
public partial class YouTubeService : IYouTubeService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<YouTubeService> _logger;
    private const string YoutubeApiBase = "https://www.googleapis.com/youtube/v3";

    public YouTubeService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<YouTubeService> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["YouTube:ApiKey"]
            ?? throw new InvalidOperationException("YouTube:ApiKey chưa được cấu hình trong appsettings.");
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<YouTubeVideoInfo?> GetVideoInfoAsync(string videoIdOrUrl, CancellationToken cancellationToken = default)
    {
        var videoId = ExtractVideoId(videoIdOrUrl);
        if (string.IsNullOrEmpty(videoId))
        {
            _logger.LogWarning("Không thể parse YouTube Video ID từ: {Input}", videoIdOrUrl);
            return null;
        }

        try
        {
            // Fetch video snippet + contentDetails (cost: 1+2 = 3 units)
            var url = $"{YoutubeApiBase}/videos?part=snippet,contentDetails&id={videoId}&key={_apiKey}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<YouTubeVideoListResponse>(json, JsonOptions);

            if (data?.Items == null || data.Items.Count == 0)
            {
                _logger.LogWarning("Video không tồn tại: {VideoId}", videoId);
                return null;
            }

            var item = data.Items[0];
            var durationSeconds = ParseIsoDuration(item.ContentDetails?.Duration);

            // Fetch caption tracks (cost: 50 units)
            var captionLanguages = await GetAvailableCaptionLanguagesAsync(videoId, cancellationToken);

            var info = new YouTubeVideoInfo
            {
                VideoId = videoId,
                Title = item.Snippet?.Title ?? string.Empty,
                Description = item.Snippet?.Description,
                ChannelTitle = item.Snippet?.ChannelTitle,
                DurationSeconds = durationSeconds,
                ThumbnailUrl = GetBestThumbnail(item.Snippet?.Thumbnails),
                HasCaptions = captionLanguages.Count > 0,
                AvailableCaptionLanguages = captionLanguages
            };

            _logger.LogInformation(
                "Fetched YouTube video info: '{Title}' ({Duration}s, captions: {CaptionCount} languages)",
                info.Title, info.DurationSeconds, info.AvailableCaptionLanguages.Count);

            return info;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi fetch YouTube video info cho: {VideoId}", videoId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<YouTubeCaptionResult?> GetCaptionsAsync(
        string videoId, string language = "en", CancellationToken cancellationToken = default)
    {
        try
        {
            // YouTube auto-generated captions có thể lấy qua timedtext endpoint (không tốn API quota)
            // Đây là public endpoint mà YouTube embed player sử dụng
            var captionUrl = $"https://www.youtube.com/api/timedtext?v={videoId}&lang={language}&fmt=srv3";
            var response = await _httpClient.GetAsync(captionUrl, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Thử auto-generated captions
                captionUrl = $"https://www.youtube.com/api/timedtext?v={videoId}&lang={language}&kind=asr&fmt=srv3";
                response = await _httpClient.GetAsync(captionUrl, cancellationToken);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Không tìm thấy captions cho video {VideoId} (lang: {Lang})", videoId, language);
                return null;
            }

            var xml = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(xml) || xml.Length < 50)
            {
                _logger.LogWarning("Caption response rỗng cho video {VideoId}", videoId);
                return null;
            }

            var segments = ParseTimedTextXml(xml);
            if (segments.Count == 0)
            {
                _logger.LogWarning("Không parse được caption segments cho video {VideoId}", videoId);
                return null;
            }

            var fullTranscript = string.Join(" ", segments.Select(s => s.Text));
            var timedTranscriptJson = JsonSerializer.Serialize(
                segments.Select(s => new { startTime = s.StartTime, endTime = s.EndTime, text = s.Text }),
                JsonOptions);

            var result = new YouTubeCaptionResult
            {
                VideoId = videoId,
                Language = language,
                IsAutoGenerated = captionUrl.Contains("kind=asr"),
                FullTranscript = fullTranscript,
                TimedTranscriptJson = timedTranscriptJson,
                Segments = segments
            };

            _logger.LogInformation(
                "Fetched {Count} caption segments for video {VideoId} (lang: {Lang}, auto: {IsAuto})",
                segments.Count, videoId, language, result.IsAutoGenerated);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi fetch captions cho video {VideoId}", videoId);
            return null;
        }
    }

    #region Private Helpers

    /// <summary>
    /// Trích xuất Video ID từ nhiều format URL khác nhau:
    /// - https://www.youtube.com/watch?v=VIDEO_ID
    /// - https://youtu.be/VIDEO_ID
    /// - https://www.youtube.com/embed/VIDEO_ID
    /// - VIDEO_ID (trực tiếp)
    /// </summary>
    internal static string? ExtractVideoId(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        input = input.Trim();

        // Đã là Video ID thuần (11 ký tự)
        if (VideoIdRegex().IsMatch(input))
            return input;

        // youtube.com/watch?v=
        var match = WatchUrlRegex().Match(input);
        if (match.Success) return match.Groups[1].Value;

        // youtu.be/
        match = ShortUrlRegex().Match(input);
        if (match.Success) return match.Groups[1].Value;

        // youtube.com/embed/
        match = EmbedUrlRegex().Match(input);
        if (match.Success) return match.Groups[1].Value;

        return null;
    }

    /// <summary>
    /// Parse ISO 8601 duration (PT1H2M30S) → seconds
    /// </summary>
    internal static int ParseIsoDuration(string? isoDuration)
    {
        if (string.IsNullOrWhiteSpace(isoDuration))
            return 0;

        var match = IsoDurationRegex().Match(isoDuration);
        if (!match.Success) return 0;

        var hours = match.Groups["hours"].Success ? int.Parse(match.Groups["hours"].Value) : 0;
        var minutes = match.Groups["minutes"].Success ? int.Parse(match.Groups["minutes"].Value) : 0;
        var seconds = match.Groups["seconds"].Success ? int.Parse(match.Groups["seconds"].Value) : 0;

        return hours * 3600 + minutes * 60 + seconds;
    }

    /// <summary>
    /// Parse YouTube timed text XML (srv3 format) → CaptionSegment list
    /// </summary>
    internal static List<CaptionSegment> ParseTimedTextXml(string xml)
    {
        var segments = new List<CaptionSegment>();

        try
        {
            var doc = XDocument.Parse(xml);
            var textElements = doc.Descendants("text");

            foreach (var el in textElements)
            {
                var startAttr = el.Attribute("start")?.Value;
                var durAttr = el.Attribute("dur")?.Value;
                var text = WebUtility.HtmlDecode(el.Value)?.Trim();

                if (string.IsNullOrWhiteSpace(text) || startAttr == null)
                    continue;

                if (!double.TryParse(startAttr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var startTime))
                    continue;

                double duration = 0;
                if (durAttr != null)
                {
                    double.TryParse(durAttr, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out duration);
                }

                // Loại bỏ các ký tự xuống dòng, format lại text sạch
                text = text.Replace("\n", " ").Replace("\r", " ");
                text = MultiSpaceRegex().Replace(text, " ").Trim();

                if (text.Length > 0)
                {
                    segments.Add(new CaptionSegment
                    {
                        StartTime = Math.Round(startTime, 1),
                        EndTime = Math.Round(startTime + duration, 1),
                        Text = text
                    });
                }
            }
        }
        catch (Exception)
        {
            // XML parse failed — trả về danh sách rỗng
        }

        return segments;
    }

    /// <summary>
    /// Lấy danh sách ngôn ngữ caption có sẵn qua YouTube Data API
    /// </summary>
    private async Task<List<string>> GetAvailableCaptionLanguagesAsync(
        string videoId, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{YoutubeApiBase}/captions?part=snippet&videoId={videoId}&key={_apiKey}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return new List<string>();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<YouTubeCaptionListResponse>(json, JsonOptions);

            return data?.Items?
                .Select(i => i.Snippet?.Language ?? "")
                .Where(l => !string.IsNullOrEmpty(l))
                .Distinct()
                .ToList() ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static string? GetBestThumbnail(YouTubeThumbnails? thumbnails)
    {
        if (thumbnails == null) return null;
        // Ưu tiên: maxres → standard → high → medium → default
        return thumbnails.Maxres?.Url
            ?? thumbnails.Standard?.Url
            ?? thumbnails.High?.Url
            ?? thumbnails.Medium?.Url
            ?? thumbnails.Default?.Url;
    }

    // Compiled Regex patterns
    [GeneratedRegex(@"^[\w-]{11}$")]
    private static partial Regex VideoIdRegex();

    [GeneratedRegex(@"[?&]v=([\w-]{11})")]
    private static partial Regex WatchUrlRegex();

    [GeneratedRegex(@"youtu\.be/([\w-]{11})")]
    private static partial Regex ShortUrlRegex();

    [GeneratedRegex(@"embed/([\w-]{11})")]
    private static partial Regex EmbedUrlRegex();

    [GeneratedRegex(@"PT(?:(?<hours>\d+)H)?(?:(?<minutes>\d+)M)?(?:(?<seconds>\d+)S)?")]
    private static partial Regex IsoDurationRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultiSpaceRegex();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    #endregion
}

#region YouTube API Response Models

internal class YouTubeVideoListResponse
{
    [JsonPropertyName("items")]
    public List<YouTubeVideoItem> Items { get; set; } = new();
}

internal class YouTubeVideoItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("snippet")]
    public YouTubeSnippet? Snippet { get; set; }

    [JsonPropertyName("contentDetails")]
    public YouTubeContentDetails? ContentDetails { get; set; }
}

internal class YouTubeSnippet
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("channelTitle")]
    public string? ChannelTitle { get; set; }

    [JsonPropertyName("thumbnails")]
    public YouTubeThumbnails? Thumbnails { get; set; }
}

internal class YouTubeThumbnails
{
    [JsonPropertyName("default")]
    public YouTubeThumbnail? Default { get; set; }

    [JsonPropertyName("medium")]
    public YouTubeThumbnail? Medium { get; set; }

    [JsonPropertyName("high")]
    public YouTubeThumbnail? High { get; set; }

    [JsonPropertyName("standard")]
    public YouTubeThumbnail? Standard { get; set; }

    [JsonPropertyName("maxres")]
    public YouTubeThumbnail? Maxres { get; set; }
}

internal class YouTubeThumbnail
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

internal class YouTubeContentDetails
{
    [JsonPropertyName("duration")]
    public string? Duration { get; set; }

    [JsonPropertyName("caption")]
    public string? Caption { get; set; }
}

internal class YouTubeCaptionListResponse
{
    [JsonPropertyName("items")]
    public List<YouTubeCaptionItem>? Items { get; set; }
}

internal class YouTubeCaptionItem
{
    [JsonPropertyName("snippet")]
    public YouTubeCaptionSnippet? Snippet { get; set; }
}

internal class YouTubeCaptionSnippet
{
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("trackKind")]
    public string? TrackKind { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

#endregion
