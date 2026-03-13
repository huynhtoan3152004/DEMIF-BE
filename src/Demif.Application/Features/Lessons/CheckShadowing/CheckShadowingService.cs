using System.Text.Json;
using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Application.Features.Lessons;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Demif.Application.Features.Lessons.CheckShadowing;

/// <summary>
/// Shadowing check service — text-fallback mode (Web Speech API).
///
/// Flow:
///   FE records audio → browser Web Speech API transcribes → sends UserText
///   Backend runs LCS word-diff (same algorithm as Dictation CheckSegmentService)
///   Returns word-level accuracy + feedback + saves UserExercise (Shadowing)
///
/// Future audio mode: pass IFormFile → call whisper.cpp process → same LCS
/// </summary>
public class CheckShadowingService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ILessonRepository _lessonRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<CheckShadowingService> _logger;

    public CheckShadowingService(
        ILessonRepository lessonRepository,
        IApplicationDbContext dbContext,
        ILogger<CheckShadowingService> logger)
    {
        _lessonRepository = lessonRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<CheckShadowingResponse>> ExecuteAsync(
        Guid lessonId,
        int segmentIndex,
        CheckShadowingRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<Level>(request.Level, ignoreCase: true, out _))
            return Result.Failure<CheckShadowingResponse>(
                Error.Validation($"Level '{request.Level}' không hợp lệ. Dùng: Beginner, Intermediate, Advanced, Expert."));

        var lesson = await _lessonRepository.GetByIdAsync(lessonId, cancellationToken);
        if (lesson is null || lesson.Status != "published")
            return Result.Failure<CheckShadowingResponse>(Error.NotFound("Không tìm thấy bài học."));

        if (string.IsNullOrWhiteSpace(lesson.TimedTranscript))
            return Result.Failure<CheckShadowingResponse>(
                Error.NotFound("Bài học chưa có Timed Transcript."));

        List<TimedSegment>? segments;
        try
        {
            segments = JsonSerializer.Deserialize<List<TimedSegment>>(lesson.TimedTranscript, JsonOptions);
        }
        catch
        {
            return Result.Failure<CheckShadowingResponse>(Error.Validation("Dữ liệu transcript không hợp lệ."));
        }

        if (segments is null || segmentIndex < 0 || segmentIndex >= segments.Count)
            return Result.Failure<CheckShadowingResponse>(
                Error.Validation($"Segment index {segmentIndex} không hợp lệ. Bài có {segments?.Count ?? 0} segments."));

        var segment = segments[segmentIndex];
        var userText = request.UserText?.Trim() ?? string.Empty;

        // LCS word-level diff (same algorithm as CheckSegmentService)
        var wordResults = CompareWords(segment.Text, userText);

        var correctCount = wordResults.Count(w => w.Status == "correct");
        var wrongCount   = wordResults.Count(w => w.Status == "wrong");
        var skippedCount = wordResults.Count(w => w.Status == "skipped");
        var totalWords   = wordResults.Count;
        var accuracy = totalWords > 0
            ? Math.Round((double)correctCount / totalWords * 100, 1)
            : 0;

        await SaveExerciseAsync(lessonId, segmentIndex, userId, request, userText, wordResults, accuracy, cancellationToken);

        _logger.LogInformation(
            "User {UserId} shadowing segment {Index} of lesson {LessonId}: {Accuracy}% ({Correct}/{Total})",
            userId, segmentIndex, lessonId, accuracy, correctCount, totalWords);

        return Result.Success(new CheckShadowingResponse
        {
            SegmentIndex = segmentIndex,
            Accuracy     = accuracy,
            CorrectCount = correctCount,
            WrongCount   = wrongCount,
            SkippedCount = skippedCount,
            TotalWords   = totalWords,
            Feedback     = GetFeedback(accuracy),
            Passed       = accuracy >= 70,
            TargetText   = segment.Text,
            UserSpoke    = userText,
            WordResults  = wordResults
        });
    }

    // ── LCS Word Alignment (same logic as CheckSegmentService) ──────────────

    private static List<ShadowingWordResult> CompareWords(string targetText, string userText)
    {
        var targetOriginal = SplitWords(targetText);
        var userOriginal   = SplitWords(userText);
        var targetNorm     = targetOriginal.Select(Normalize).ToArray();
        var userNorm       = userOriginal.Select(Normalize).ToArray();

        var n = targetNorm.Length;
        var m = userNorm.Length;

        if (n == 0)
            return new List<ShadowingWordResult>();

        if (m == 0)
            return targetOriginal.Select(w => new ShadowingWordResult { Word = w, Status = "skipped" }).ToList();

        // Build LCS DP table
        var dp = new int[n + 1, m + 1];
        for (var i = 1; i <= n; i++)
            for (var j = 1; j <= m; j++)
                dp[i, j] = targetNorm[i - 1] == userNorm[j - 1]
                    ? dp[i - 1, j - 1] + 1
                    : Math.Max(dp[i - 1, j], dp[i, j - 1]);

        // Backtrack to find matched indices
        var tMatched = new int[n];
        Array.Fill(tMatched, -1);

        int ti = n, ui = m;
        while (ti > 0 && ui > 0)
        {
            if (targetNorm[ti - 1] == userNorm[ui - 1])
            {
                tMatched[ti - 1] = ui - 1;
                ti--; ui--;
            }
            else if (dp[ti - 1, ui] >= dp[ti, ui - 1])
                ti--;
            else
                ui--;
        }

        // Build results
        var userAssigned = new bool[m];
        foreach (var mi in tMatched.Where(mi => mi >= 0))
            userAssigned[mi] = true;

        var results = new List<ShadowingWordResult>(n);
        var lastMatchedUserIdx = 0;

        for (var i = 0; i < n; i++)
        {
            if (tMatched[i] >= 0)
            {
                results.Add(new ShadowingWordResult { Word = targetOriginal[i], Status = "correct" });
                lastMatchedUserIdx = tMatched[i] + 1;
            }
            else
            {
                var nextMatchUserIdx = i < n - 1 && tMatched[i + 1] >= 0 ? tMatched[i + 1] : m;
                var unassignedNear = Enumerable.Range(lastMatchedUserIdx, nextMatchUserIdx - lastMatchedUserIdx)
                    .FirstOrDefault(j => !userAssigned[j], -1);

                if (unassignedNear >= 0)
                {
                    userAssigned[unassignedNear] = true;
                    results.Add(new ShadowingWordResult
                    {
                        Word       = targetOriginal[i],
                        Status     = "wrong",
                        UserSpoken = userOriginal[unassignedNear]
                    });
                }
                else
                {
                    results.Add(new ShadowingWordResult { Word = targetOriginal[i], Status = "skipped" });
                }
            }
        }

        return results;
    }

    private static string[] SplitWords(string text)
        => string.IsNullOrWhiteSpace(text)
            ? Array.Empty<string>()
            : text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

    private static string Normalize(string word)
        => word
            .TrimEnd('.', ',', '!', '?', ';', ':', '\'', '"', ')')
            .TrimStart('\'', '"', '(')
            .ToLowerInvariant();

    private static string GetFeedback(double accuracy) => accuracy switch
    {
        >= 95 => "🌟 Excellent! Your pronunciation is very accurate.",
        >= 80 => "✅ Good job! A few words need more practice.",
        >= 70 => "📖 Not bad. Listen again and pay attention to intonation.",
        >= 50 => "💪 Keep practicing. Try shadowing word by word.",
        _     => "🎧 Listen carefully and try again."
    };

    // ── Save UserExercise ────────────────────────────────────────────────────

    private async Task SaveExerciseAsync(
        Guid lessonId,
        int segmentIndex,
        Guid userId,
        CheckShadowingRequest request,
        string userText,
        List<ShadowingWordResult> wordResults,
        double accuracy,
        CancellationToken cancellationToken)
    {
        try
        {
            var userInput = JsonSerializer.Serialize(new
            {
                segmentIndex,
                userText,
                level = request.Level
            });
            var resultDetails = JsonSerializer.Serialize(new
            {
                segmentIndex,
                accuracy,
                wordResults
            });
            var newScore = (int)Math.Round(accuracy);

            var existing = await _dbContext.UserExercises
                .FirstOrDefaultAsync(
                    e => e.UserId      == userId
                      && e.LessonId    == lessonId
                      && e.SegmentIndex == segmentIndex
                      && e.ExerciseType == ExerciseType.Shadowing,
                    cancellationToken);

            if (existing is not null)
            {
                existing.Attempts++;
                existing.Score         = Math.Max(existing.Score, newScore);
                existing.UserInput     = userInput;
                existing.ResultDetails = resultDetails;
                existing.TimeSpentSeconds = request.TimeSpentSeconds;
                existing.CompletedAt   = DateTime.UtcNow;
                _dbContext.UserExercises.Update(existing);
            }
            else
            {
                _dbContext.UserExercises.Add(new UserExercise
                {
                    UserId           = userId,
                    LessonId         = lessonId,
                    ExerciseType     = ExerciseType.Shadowing,
                    SegmentIndex     = segmentIndex,
                    UserInput        = userInput,
                    ResultDetails    = resultDetails,
                    Score            = newScore,
                    TimeSpentSeconds = request.TimeSpentSeconds,
                    CompletedAt      = DateTime.UtcNow
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to save UserExercise for shadowing (lesson {LessonId}, segment {Index})",
                lessonId, segmentIndex);
        }
    }
}
