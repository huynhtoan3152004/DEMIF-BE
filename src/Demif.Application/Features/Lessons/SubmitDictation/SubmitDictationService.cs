using System.Text.Json;
using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Demif.Application.Features.Lessons.SubmitDictation;

/// <summary>
/// Service xử lý submission dictation exercise + chấm điểm.
/// So sánh case-insensitive, loại bỏ dấu câu, tính % chính xác.
/// </summary>
public class SubmitDictationService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<SubmitDictationService> _logger;

    public SubmitDictationService(
        ILessonRepository lessonRepository,
        IApplicationDbContext dbContext,
        ILogger<SubmitDictationService> logger)
    {
        _lessonRepository = lessonRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<DictationSubmitResponse>> ExecuteAsync(
        Guid lessonId,
        Guid userId,
        DictationSubmitRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Tìm lesson
        var lesson = await _lessonRepository.GetByIdAsync(lessonId, cancellationToken);
        if (lesson is null)
            return Result.Failure<DictationSubmitResponse>(Error.NotFound("Không tìm thấy bài học."));

        if (string.IsNullOrWhiteSpace(lesson.DictationTemplates))
            return Result.Failure<DictationSubmitResponse>(Error.NotFound("Bài học chưa có Dictation template."));

        // 2. Parse level
        if (!Enum.TryParse<Level>(request.Level, true, out var level))
            return Result.Failure<DictationSubmitResponse>(Error.Validation($"Level '{request.Level}' không hợp lệ."));

        // 3. Lấy template cho level
        var template = DictationTemplateGenerator.GetTemplateForLevel(lesson.DictationTemplates, level);
        if (template is null)
            return Result.Failure<DictationSubmitResponse>(Error.NotFound($"Không tìm thấy template cho level '{level}'."));

        // 4. Chấm điểm từng answer
        var results = new List<AnswerResult>();
        var correctCount = 0;

        foreach (var userAnswer in request.Answers)
        {
            // Tìm segment + word tương ứng
            if (userAnswer.SegmentIndex < 0 || userAnswer.SegmentIndex >= template.Segments.Count)
            {
                results.Add(new AnswerResult
                {
                    SegmentIndex = userAnswer.SegmentIndex,
                    Position = userAnswer.Position,
                    IsCorrect = false,
                    UserInput = userAnswer.UserInput,
                    CorrectAnswer = "?",
                    Message = "Segment index không hợp lệ."
                });
                continue;
            }

            var segment = template.Segments[userAnswer.SegmentIndex];
            var blankWord = segment.Words.FirstOrDefault(w => w.IsBlank && w.Position == userAnswer.Position);

            if (blankWord is null)
            {
                results.Add(new AnswerResult
                {
                    SegmentIndex = userAnswer.SegmentIndex,
                    Position = userAnswer.Position,
                    IsCorrect = false,
                    UserInput = userAnswer.UserInput,
                    CorrectAnswer = "?",
                    Message = "Vị trí blank không hợp lệ."
                });
                continue;
            }

            // So sánh case-insensitive, loại bỏ dấu câu + khoảng trắng thừa
            var isCorrect = NormalizeForComparison(userAnswer.UserInput) ==
                            NormalizeForComparison(blankWord.Answer ?? "");

            if (isCorrect) correctCount++;

            results.Add(new AnswerResult
            {
                SegmentIndex = userAnswer.SegmentIndex,
                Position = userAnswer.Position,
                IsCorrect = isCorrect,
                UserInput = userAnswer.UserInput.Trim(),
                CorrectAnswer = blankWord.Answer ?? "",
                Message = isCorrect ? "Chính xác!" : null
            });
        }

        // 5. Tính điểm
        var totalBlanks = template.TotalBlanks;
        var answeredBlanks = request.Answers.Count;
        var score = totalBlanks > 0
            ? (decimal)correctCount / totalBlanks * 100
            : 0;

        // 6. Lưu kết quả vào UserExercise — UPSERT pattern (tránh lỗi 500 khi submit lại)
        var userInputJson = JsonSerializer.Serialize(request.Answers, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var resultDetailsJson = JsonSerializer.Serialize(results, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var newScore = (int)Math.Round(score);

        // Tìm existing exercise cho cùng user + lesson + Dictation (toàn bài, SegmentIndex == null)
        var existing = await _dbContext.UserExercises
            .FirstOrDefaultAsync(
                e => e.UserId == userId
                  && e.LessonId == lessonId
                  && e.ExerciseType == ExerciseType.Dictation
                  && e.SegmentIndex == null,
                cancellationToken);

        UserExercise exercise;
        if (existing is not null)
        {
            existing.Attempts++;
            existing.Score = Math.Max(existing.Score, newScore);
            existing.UserInput = userInputJson;
            existing.ResultDetails = resultDetailsJson;
            existing.TimeSpentSeconds = request.TimeSpentSeconds;
            existing.CompletedAt = DateTime.UtcNow;
            _dbContext.UserExercises.Update(existing);
            exercise = existing;
        }
        else
        {
            exercise = new UserExercise
            {
                UserId = userId,
                LessonId = lessonId,
                ExerciseType = ExerciseType.Dictation,
                UserInput = userInputJson,
                ResultDetails = resultDetailsJson,
                Score = newScore,
                TimeSpentSeconds = request.TimeSpentSeconds,
                CompletedAt = DateTime.UtcNow
            };
            _dbContext.UserExercises.Add(exercise);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Update denormalized stats on Lesson
        await UpdateLessonStatsAsync(lesson, lessonId, cancellationToken);

        _logger.LogInformation(
            "User {UserId} submitted dictation for lesson {LessonId} (Level: {Level}): {Score}% ({Correct}/{Total})",
            userId, lessonId, level, Math.Round(score, 1), correctCount, totalBlanks);

        return Result.Success(new DictationSubmitResponse
        {
            ExerciseId = exercise.Id,
            Score = Math.Round(score, 1),
            TotalBlanks = totalBlanks,
            AnsweredBlanks = answeredBlanks,
            CorrectCount = correctCount,
            IncorrectCount = answeredBlanks - correctCount,
            SkippedCount = totalBlanks - answeredBlanks,
            Level = level.ToString(),
            Results = results,
            TimeSpentSeconds = request.TimeSpentSeconds
        });
    }

    /// <summary>
    /// Recalculate and persist CompletionsCount + AvgScore for a lesson.
    /// Runs after every successful submission to keep denormalized stats in sync.
    /// </summary>
    private async Task UpdateLessonStatsAsync(Lesson lesson, Guid lessonId, CancellationToken cancellationToken)
    {
        try
        {
            var stats = await _dbContext.UserExercises
                .Where(e => e.LessonId == lessonId && e.ExerciseType == ExerciseType.Dictation)
                .GroupBy(_ => 1)
                .Select(g => new { Count = g.Count(), Avg = (decimal)g.Average(e => e.Score) })
                .FirstOrDefaultAsync(cancellationToken);

            if (stats is null) return;

            lesson.CompletionsCount = stats.Count;
            lesson.AvgScore = Math.Round(stats.Avg, 1);

            await _lessonRepository.UpdateAsync(lesson, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Stats update is non-critical — do not fail the submission
            _logger.LogWarning(ex, "Failed to update lesson stats for {LessonId}", lessonId);
        }
    }

    /// <summary>
    /// Normalize string: lowercase, trim, bỏ dấu câu đầu/cuối
    /// "Powerful." → "powerful", "  Hello " → "hello"
    /// </summary>
    private static string NormalizeForComparison(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";

        return input.Trim()
            .TrimEnd('.', ',', '!', '?', ';', ':', '\'', '"')
            .TrimStart('\'', '"')
            .ToLowerInvariant();
    }
}

#region Request/Response DTOs

/// <summary>
/// Request body khi user submit dictation
/// </summary>
public class DictationSubmitRequest
{
    /// <summary>
    /// Level đã chọn: "Beginner", "Intermediate", "Advanced", "Expert"
    /// </summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// Danh sách câu trả lời cho từng blank
    /// </summary>
    public List<UserAnswerInput> Answers { get; set; } = new();

    /// <summary>
    /// Thời gian user làm bài (giây)
    /// </summary>
    public int TimeSpentSeconds { get; set; }
}

/// <summary>
/// Một câu trả lời cho 1 blank
/// </summary>
public class UserAnswerInput
{
    /// <summary>
    /// Index của segment chứa blank (0-based)
    /// </summary>
    public int SegmentIndex { get; set; }

    /// <summary>
    /// Vị trí từ trong segment (match với DictationWord.Position)
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Từ user điền vào
    /// </summary>
    public string UserInput { get; set; } = string.Empty;
}

/// <summary>
/// Response sau khi chấm điểm
/// </summary>
public class DictationSubmitResponse
{
    public Guid ExerciseId { get; set; }
    public decimal Score { get; set; }
    public int TotalBlanks { get; set; }
    public int AnsweredBlanks { get; set; }
    public int CorrectCount { get; set; }
    public int IncorrectCount { get; set; }
    public int SkippedCount { get; set; }
    public string Level { get; set; } = string.Empty;
    public int TimeSpentSeconds { get; set; }
    public List<AnswerResult> Results { get; set; } = new();
}

/// <summary>
/// Kết quả cho từng blank
/// </summary>
public class AnswerResult
{
    public int SegmentIndex { get; set; }
    public int Position { get; set; }
    public bool IsCorrect { get; set; }
    public string UserInput { get; set; } = string.Empty;

    /// <summary>
    /// Đáp án đúng — chỉ trả về sau khi submit
    /// </summary>
    public string CorrectAnswer { get; set; } = string.Empty;

    public string? Message { get; set; }
}

#endregion
