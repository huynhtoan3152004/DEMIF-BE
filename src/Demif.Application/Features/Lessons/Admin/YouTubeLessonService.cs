using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Demif.Application.Features.Lessons.Admin;

/// <summary>
/// Service tạo lesson từ YouTube URL.
/// 
/// Flow:
/// 1. Admin paste YouTube URL
/// 2. Fetch video metadata (title, thumbnail, duration) 
/// 3. Fetch captions → FullTranscript + TimedTranscript
/// 4. Auto-generate DictationTemplates cho 4 levels (Beginner → Expert)
/// 5. Tạo Lesson entity với đầy đủ data
/// 
/// User experience:
/// - Video embed trong app (iframe YouTube player)
/// - Player sync với TimedTranscript segments
/// - User nghe đoạn → điền từ bị ẩn → submit chấm điểm
/// </summary>
public class YouTubeLessonService
{
    private readonly IYouTubeService _youTubeService;
    private readonly ILessonRepository _lessonRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<YouTubeLessonService> _logger;

    public YouTubeLessonService(
        IYouTubeService youTubeService,
        ILessonRepository lessonRepository,
        IApplicationDbContext dbContext,
        ILogger<YouTubeLessonService> logger)
    {
        _youTubeService = youTubeService;
        _lessonRepository = lessonRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Preview YouTube video trước khi tạo lesson.
    /// Admin xem info → quyết định tạo hay không.
    /// </summary>
    public async Task<Result<YouTubePreviewResponse>> PreviewAsync(
        string youtubeUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(youtubeUrl))
            return Result.Failure<YouTubePreviewResponse>(Error.Validation("YouTube URL không được để trống."));

        var videoInfo = await _youTubeService.GetVideoInfoAsync(youtubeUrl, cancellationToken);
        if (videoInfo is null)
            return Result.Failure<YouTubePreviewResponse>(
                Error.Validation("Không thể lấy thông tin video. Kiểm tra lại URL hoặc video có thể bị private/xóa."));

        return Result.Success(new YouTubePreviewResponse
        {
            VideoId = videoInfo.VideoId,
            Title = videoInfo.Title,
            Description = TruncateDescription(videoInfo.Description),
            ChannelTitle = videoInfo.ChannelTitle,
            DurationSeconds = videoInfo.DurationSeconds,
            ThumbnailUrl = videoInfo.ThumbnailUrl,
            EmbedUrl = videoInfo.EmbedUrl,
            HasCaptions = videoInfo.HasCaptions,
            AvailableCaptionLanguages = videoInfo.AvailableCaptionLanguages,
            SuggestedCategory = SuggestCategory(videoInfo)
        });
    }

    /// <summary>
    /// Tạo lesson từ YouTube URL — full auto pipeline.
    /// </summary>
    public async Task<Result<CreateLessonFromYouTubeResponse>> CreateFromYouTubeAsync(
        CreateLessonFromYouTubeRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Validate input
        if (string.IsNullOrWhiteSpace(request.YouTubeUrl))
            return Result.Failure<CreateLessonFromYouTubeResponse>(
                Error.Validation("YouTube URL không được để trống."));

        // 2. Fetch video metadata
        var videoInfo = await _youTubeService.GetVideoInfoAsync(request.YouTubeUrl, cancellationToken);
        if (videoInfo is null)
            return Result.Failure<CreateLessonFromYouTubeResponse>(
                Error.Validation("Không thể lấy thông tin video. Kiểm tra lại URL hoặc video bị private/xóa."));

        if (videoInfo.DurationSeconds <= 0)
            return Result.Failure<CreateLessonFromYouTubeResponse>(
                Error.Validation("Không xác định được thời lượng video."));

        // 3. Fetch captions
        var captions = await _youTubeService.GetCaptionsAsync(
            videoInfo.VideoId, request.CaptionLanguage, cancellationToken);

        var hasCaptions = captions != null && !string.IsNullOrWhiteSpace(captions.FullTranscript);

        // 4. Build Lesson entity
        var lesson = new Lesson
        {
            Title = request.TitleOverride ?? videoInfo.Title,
            Description = request.DescriptionOverride ?? TruncateDescription(videoInfo.Description),
            LessonType = request.LessonType,
            Level = request.Level,
            Category = request.Category,

            // YouTube embed URL cho player
            AudioUrl = videoInfo.EmbedUrl,
            MediaUrl = videoInfo.EmbedUrl,
            MediaType = "youtube",

            DurationSeconds = videoInfo.DurationSeconds,
            ThumbnailUrl = videoInfo.ThumbnailUrl,

            // Transcript từ captions
            FullTranscript = hasCaptions ? captions!.FullTranscript : string.Empty,
            TimedTranscript = hasCaptions ? captions!.TimedTranscriptJson : null,

            IsPremiumOnly = request.IsPremiumOnly,
            DisplayOrder = request.DisplayOrder,
            Tags = request.Tags,
            Status = request.Status
        };

        // 5. Auto-generate DictationTemplates (nếu có captions)
        if (hasCaptions && !string.IsNullOrWhiteSpace(lesson.TimedTranscript))
        {
            try
            {
                lesson.DictationTemplates = DictationTemplateGenerator.GenerateAllTemplates(lesson.TimedTranscript);
                _logger.LogInformation(
                    "Generated DictationTemplates cho YouTube video '{VideoId}' ({Segments} segments)",
                    videoInfo.VideoId, captions!.Segments.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Không thể generate DictationTemplates cho video {VideoId}", videoInfo.VideoId);
                // Không throw — lesson vẫn tạo được, admin có thể regenerate sau
            }
        }
        else
        {
            _logger.LogWarning(
                "Video '{VideoId}' không có captions (lang: {Lang}). Lesson sẽ không có Dictation exercise.",
                videoInfo.VideoId, request.CaptionLanguage);
        }

        // 6. Save
        await _lessonRepository.AddAsync(lesson, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created lesson '{Title}' from YouTube video {VideoId} (Id: {LessonId}, captions: {HasCaptions})",
            lesson.Title, videoInfo.VideoId, lesson.Id, hasCaptions);

        // 7. Build response
        var transcriptWords = hasCaptions
            ? captions!.FullTranscript.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length
            : 0;

        var response = new CreateLessonFromYouTubeResponse
        {
            LessonId = lesson.Id,
            Title = lesson.Title,
            VideoId = videoInfo.VideoId,
            EmbedUrl = videoInfo.EmbedUrl,
            DurationSeconds = lesson.DurationSeconds,
            ThumbnailUrl = lesson.ThumbnailUrl,
            HasCaptions = hasCaptions,
            CaptionLanguage = hasCaptions ? captions!.Language : null,
            IsAutoGeneratedCaptions = hasCaptions && captions!.IsAutoGenerated,
            CaptionSegmentCount = hasCaptions ? captions!.Segments.Count : 0,
            TranscriptWordCount = transcriptWords,
            HasDictationTemplates = !string.IsNullOrWhiteSpace(lesson.DictationTemplates),
            Status = lesson.Status,
            Message = BuildResultMessage(hasCaptions, captions, lesson)
        };

        return Result.Success(response);
    }

    #region Helpers

    private static string BuildResultMessage(bool hasCaptions, YouTubeCaptionResult? captions, Lesson lesson)
    {
        if (!hasCaptions)
            return "⚠️ Lesson đã tạo nhưng KHÔNG có captions. Bạn cần thêm transcript thủ công để sử dụng Dictation.";

        if (string.IsNullOrWhiteSpace(lesson.DictationTemplates))
            return "⚠️ Lesson đã tạo có captions nhưng không thể generate DictationTemplates. Thử regenerate sau.";

        var autoNote = captions!.IsAutoGenerated ? " (auto-generated, có thể có lỗi nhỏ)" : " (manual, chất lượng cao)";
        return $"✅ Lesson đã tạo thành công với Dictation templates cho 4 levels! Captions: {captions.Language}{autoNote}";
    }

    private static string? TruncateDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description)) return null;
        return description.Length > 500 ? description[..500] + "..." : description;
    }

    /// <summary>
    /// Đoán category từ video info (heuristic đơn giản)
    /// </summary>
    private static string? SuggestCategory(YouTubeVideoInfo info)
    {
        var text = $"{info.Title} {info.Description} {info.ChannelTitle}".ToLowerInvariant();

        if (text.Contains("ted") || text.Contains("lecture") || text.Contains("academic"))
            return "academic";
        if (text.Contains("business") || text.Contains("entrepreneur") || text.Contains("startup"))
            return "business";
        if (text.Contains("travel") || text.Contains("tourism") || text.Contains("trip"))
            return "travel";
        if (text.Contains("news") || text.Contains("bbc") || text.Contains("cnn"))
            return "news";
        if (text.Contains("ielts") || text.Contains("toefl") || text.Contains("toeic"))
            return "exam";
        if (text.Contains("movie") || text.Contains("film") || text.Contains("trailer"))
            return "entertainment";

        return "conversation";
    }

    #endregion
}
