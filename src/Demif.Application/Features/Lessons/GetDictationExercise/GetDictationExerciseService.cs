using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Demif.Application.Features.Lessons.GetDictationExercise;

/// <summary>
/// Service lấy dictation exercise cho user.
/// GET /api/lessons/{id}/dictation?level=Beginner
/// </summary>
public class GetDictationExerciseService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IApplicationDbContext _dbContext;

    public GetDictationExerciseService(
        ILessonRepository lessonRepository,
        IApplicationDbContext dbContext)
    {
        _lessonRepository = lessonRepository;
        _dbContext = dbContext;
    }

    public async Task<Result<DictationExerciseResponse>> ExecuteAsync(
        Guid lessonId,
        Level level,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        var lesson = await _lessonRepository.GetByIdAsync(lessonId, cancellationToken);
        if (lesson is null)
        {
            return Result.Failure<DictationExerciseResponse>(Error.NotFound("Không tìm thấy bài học."));
        }

        // Kiểm tra premium access
        if (lesson.IsPremiumOnly && userId.HasValue)
        {
            var hasPremium = await _dbContext.UserRoles
                .AnyAsync(ur => ur.UserId == userId.Value &&
                    ur.Role.Name == "Premium" &&
                    (ur.ExpiresAt == null || ur.ExpiresAt > DateTime.UtcNow), cancellationToken);

            if (!hasPremium)
            {
                return Result.Failure<DictationExerciseResponse>(
                    Error.Forbidden("Bài học này chỉ dành cho Premium. Vui lòng nâng cấp tài khoản."));
            }
        }
        else if (lesson.IsPremiumOnly && !userId.HasValue)
        {
            return Result.Failure<DictationExerciseResponse>(
                Error.Forbidden("Bài học này chỉ dành cho Premium. Vui lòng đăng nhập và nâng cấp tài khoản."));
        }

        // Kiểm tra lesson có DictationTemplates
        if (string.IsNullOrWhiteSpace(lesson.DictationTemplates))
        {
            return Result.Failure<DictationExerciseResponse>(
                Error.NotFound("Bài học chưa có Dictation template. Vui lòng liên hệ admin."));
        }

        // Lấy template cho level yêu cầu
        var template = DictationTemplateGenerator.GetTemplateForLevel(lesson.DictationTemplates, level);
        if (template is null)
        {
            return Result.Failure<DictationExerciseResponse>(
                Error.NotFound($"Không tìm thấy template cho level '{level}'."));
        }

        // ⚠️ QUAN TRỌNG: Xóa Answer khỏi response để user không cheat
        StripAnswers(template);

        return Result.Success(new DictationExerciseResponse
        {
            LessonId = lesson.Id,
            Title = lesson.Title,
            Description = lesson.Description,
            AudioUrl = lesson.MediaUrl ?? lesson.AudioUrl,
            DurationSeconds = lesson.DurationSeconds,
            Level = level.ToString(),
            Template = template,
            ThumbnailUrl = lesson.ThumbnailUrl
        });
    }

    /// <summary>
    /// Xóa field Answer khỏi template trước khi gửi cho frontend.
    /// User chỉ nhận được Hint + Length, không nhận được đáp án.
    /// </summary>
    private static void StripAnswers(DictationTemplate template)
    {
        foreach (var segment in template.Segments)
        {
            foreach (var word in segment.Words)
            {
                if (word.IsBlank)
                {
                    word.Answer = null; // Không gửi đáp án ra frontend
                }
            }
        }
    }
}

/// <summary>
/// Response cho dictation exercise
/// </summary>
public class DictationExerciseResponse
{
    public Guid LessonId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AudioUrl { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public string Level { get; set; } = string.Empty;
    public DictationTemplate Template { get; set; } = null!;
    public string? ThumbnailUrl { get; set; }
}
