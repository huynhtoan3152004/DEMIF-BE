using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Demif.Application.Features.Lessons.Admin;

/// <summary>
/// Service upload audio MP3 lên Firebase Storage và gắn vào lesson.
/// POST /api/admin/lessons/{id}/upload-audio
/// </summary>
public class UploadLessonAudioService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IFirebaseStorageService _storageService;
    private readonly ILogger<UploadLessonAudioService> _logger;

    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB

    public UploadLessonAudioService(
        ILessonRepository lessonRepository,
        IApplicationDbContext dbContext,
        IFirebaseStorageService storageService,
        ILogger<UploadLessonAudioService> logger)
    {
        _lessonRepository = lessonRepository;
        _dbContext = dbContext;
        _storageService = storageService;
        _logger = logger;
    }

    public async Task<Result<UploadAudioResponse>> ExecuteAsync(
        Guid lessonId,
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        // 1. Kiểm tra lesson tồn tại
        var lesson = await _lessonRepository.GetByIdAsync(lessonId, cancellationToken);
        if (lesson is null)
            return Result.Failure<UploadAudioResponse>(Error.NotFound("Không tìm thấy bài học."));

        // 2. Validate file size
        if (fileStream.Length > MaxFileSizeBytes)
            return Result.Failure<UploadAudioResponse>(
                Error.Validation($"File quá lớn. Tối đa 50MB, file hiện tại: {fileStream.Length / 1024 / 1024}MB."));

        // 3. Validate content type
        var allowedTypes = new[] { "audio/mpeg", "audio/mp3", "audio/wav", "audio/ogg", "audio/x-m4a" };
        if (!allowedTypes.Contains(contentType.ToLower()))
            return Result.Failure<UploadAudioResponse>(
                Error.Validation($"Chỉ chấp nhận file audio. Content-type nhận được: '{contentType}'."));

        try
        {
            // 4. Upload lên Firebase Storage
            var audioUrl = await _storageService.UploadAudioAsync(
                fileStream, fileName, contentType, lessonId.ToString(), cancellationToken);

            // 5. Cập nhật lesson với URL mới
            lesson.AudioUrl = audioUrl;
            lesson.MediaUrl = audioUrl;
            lesson.MediaType = "audio";

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Audio uploaded for lesson {LessonId}: {Url}", lessonId, audioUrl);

            return Result.Success(new UploadAudioResponse
            {
                LessonId = lessonId,
                AudioUrl = audioUrl,
                FileName = fileName,
                FileSizeBytes = fileStream.Length
            });
        }
        catch (ArgumentException ex)
        {
            return Result.Failure<UploadAudioResponse>(Error.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload audio for lesson {LessonId}", lessonId);
            return Result.Failure<UploadAudioResponse>(
                Error.Internal("Upload thất bại. Vui lòng thử lại."));
        }
    }
}

/// <summary>
/// Response sau khi upload audio thành công
/// </summary>
public class UploadAudioResponse
{
    public Guid LessonId { get; set; }
    public string AudioUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}
