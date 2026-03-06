using System.Text.Json;
using Demif.Application.Abstractions.Persistence;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Demif.Application.Features.Lessons.CheckSegment;

/// <summary>
/// Service chấm điểm tự do cho 1 segment: so sánh user text với transcript gốc.
/// 
/// Algorithm: LCS (Longest Common Subsequence) word alignment
/// - Normalize: lowercase + bỏ dấu câu
/// - Align bằng LCS để handle skip/insert gracefully
/// - Mark: correct | wrong | skipped
/// 
/// Luôn trả transcript sau check — đây là học liệu học từ lỗi (Option A + C).
/// </summary>
public class CheckSegmentService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ILogger<CheckSegmentService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CheckSegmentService(
        ILessonRepository lessonRepository,
        IApplicationDbContext dbContext,
        ILogger<CheckSegmentService> logger)
    {
        _lessonRepository = lessonRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<CheckSegmentResponse>> ExecuteAsync(
        Guid lessonId,
        int segmentIndex,
        CheckSegmentRequest request,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<Level>(request.Level, ignoreCase: true, out var level))
            return Result.Failure<CheckSegmentResponse>(
                Error.Validation($"Level '{request.Level}' không hợp lệ."));

        var lesson = await _lessonRepository.GetByIdAsync(lessonId, cancellationToken);
        if (lesson is null || lesson.Status != "published")
            return Result.Failure<CheckSegmentResponse>(Error.NotFound("Không tìm thấy bài học."));

        if (string.IsNullOrWhiteSpace(lesson.TimedTranscript))
            return Result.Failure<CheckSegmentResponse>(
                Error.NotFound("Bài học chưa có Timed Transcript."));

        List<TimedSegment>? segments;
        try
        {
            segments = JsonSerializer.Deserialize<List<TimedSegment>>(lesson.TimedTranscript, JsonOptions);
        }
        catch
        {
            return Result.Failure<CheckSegmentResponse>(Error.Validation("Dữ liệu transcript không hợp lệ."));
        }

        if (segments is null || segmentIndex < 0 || segmentIndex >= segments.Count)
            return Result.Failure<CheckSegmentResponse>(
                Error.Validation($"Segment index {segmentIndex} không hợp lệ. Bài có {segments?.Count ?? 0} segments."));

        var segment = segments[segmentIndex];

        // Core: word-by-word diff với LCS alignment
        var wordResults = CompareWords(segment.Text, request.UserText ?? "");

        var correctCount = wordResults.Count(w => w.Status == "correct");
        var wrongCount = wordResults.Count(w => w.Status == "wrong");
        var skippedCount = wordResults.Count(w => w.Status == "skipped");
        var totalWords = wordResults.Count;
        var accuracy = totalWords > 0
            ? Math.Round((double)correctCount / totalWords * 100, 1)
            : 0;

        // Lưu UserExercise (type = Dictation, dùng chung với flow cũ)
        await SaveExerciseAsync(lessonId, segmentIndex, userId, request, wordResults, accuracy, cancellationToken);

        _logger.LogInformation(
            "User {UserId} checked segment {Index} of lesson {LessonId} (Level: {Level}): {Accuracy}% ({Correct}/{Total})",
            userId, segmentIndex, lessonId, level, accuracy, correctCount, totalWords);

        return Result.Success(new CheckSegmentResponse
        {
            SegmentIndex = segmentIndex,
            Accuracy = accuracy,
            CorrectCount = correctCount,
            TotalWords = totalWords,
            WrongCount = wrongCount,
            SkippedCount = skippedCount,
            Transcript = segment.Text,       // Luôn trả — học từ lỗi
            WordResults = wordResults
        });
    }

    /// <summary>
    /// LCS-based word alignment.
    /// Xử lý đúng trường hợp user bỏ qua từ hoặc thêm từ thừa.
    /// 
    /// Ví dụ:
    ///   Transcript: "Hello everyone welcome to class"
    ///   User typed: "Hello everone welcome class"
    ///   Result: correct | wrong(everone) | correct | skipped | correct
    /// </summary>
    private static List<WordCheckResult> CompareWords(string transcriptText, string userText)
    {
        // Tách thành 2 mảng: gốc (preserve case) và normalized (so sánh)
        var transcriptOriginal = SplitWords(transcriptText);
        var userOriginal = SplitWords(userText);
        var transcriptNorm = transcriptOriginal.Select(Normalize).ToArray();
        var userNorm = userOriginal.Select(Normalize).ToArray();

        var n = transcriptNorm.Length;
        var m = userNorm.Length;

        if (n == 0)
            return new List<WordCheckResult>();

        if (m == 0)
            return transcriptOriginal.Select(w => new WordCheckResult
            {
                Word = w,
                Status = "skipped"
            }).ToList();

        // Build LCS DP table
        var dp = new int[n + 1, m + 1];
        for (var i = 1; i <= n; i++)
            for (var j = 1; j <= m; j++)
                dp[i, j] = transcriptNorm[i - 1] == userNorm[j - 1]
                    ? dp[i - 1, j - 1] + 1
                    : Math.Max(dp[i - 1, j], dp[i, j - 1]);

        // Backtrack để lấy matched indices
        var tMatched = new int[n]; // tMatched[i] = index trong userNorm, -1 nếu không match
        Array.Fill(tMatched, -1);

        int ti = n, ui = m;
        while (ti > 0 && ui > 0)
        {
            if (transcriptNorm[ti - 1] == userNorm[ui - 1])
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
        // Với mỗi transcript word:
        //   - Nếu matched → correct
        //   - Nếu không matched → tìm user word chưa assign gần nhất để mark "wrong"
        //     (user gõ sai chính tả nhưng vẫn có gõ gì đó ở vị trí đó)
        //   - Nếu không có user word nào → skipped
        var userAssigned = new bool[m];
        // Đánh dấu những user word đã được LCS assign
        foreach (var mi in tMatched.Where(mi => mi >= 0))
            userAssigned[mi] = true;

        var results = new List<WordCheckResult>(n);
        var lastMatchedUserIdx = 0;

        for (var i = 0; i < n; i++)
        {
            if (tMatched[i] >= 0)
            {
                // Perfect match
                results.Add(new WordCheckResult
                {
                    Word = transcriptOriginal[i],
                    Status = "correct"
                });
                lastMatchedUserIdx = tMatched[i] + 1;
            }
            else
            {
                // Tìm user word chưa assign trong khoảng [lastMatchedUserIdx, nextMatch)
                var nextMatchUserIdx = i < n - 1 && tMatched[i + 1] >= 0 ? tMatched[i + 1] : m;
                var unassignedNear = Enumerable.Range(lastMatchedUserIdx, nextMatchUserIdx - lastMatchedUserIdx)
                    .FirstOrDefault(j => !userAssigned[j], -1);

                if (unassignedNear >= 0)
                {
                    // User gõ sai
                    userAssigned[unassignedNear] = true;
                    results.Add(new WordCheckResult
                    {
                        Word = transcriptOriginal[i],
                        Status = "wrong",
                        UserTyped = userOriginal[unassignedNear]
                    });
                }
                else
                {
                    // User bỏ qua
                    results.Add(new WordCheckResult
                    {
                        Word = transcriptOriginal[i],
                        Status = "skipped"
                    });
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Split text thành words, bỏ token rỗng.
    /// </summary>
    private static string[] SplitWords(string text)
        => string.IsNullOrWhiteSpace(text)
            ? Array.Empty<string>()
            : text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// Normalize để so sánh: lowercase + bỏ dấu câu đầu/cuối mỗi từ.
    /// "Everyone." → "everyone", "WELCOME" → "welcome"
    /// </summary>
    private static string Normalize(string word)
        => word
            .TrimEnd('.', ',', '!', '?', ';', ':', '\'', '"', ')')
            .TrimStart('\'', '"', '(')
            .ToLowerInvariant();

    private async Task SaveExerciseAsync(
        Guid lessonId,
        int segmentIndex,
        Guid userId,
        CheckSegmentRequest request,
        List<WordCheckResult> wordResults,
        double accuracy,
        CancellationToken cancellationToken)
    {
        try
        {
            var userInput = JsonSerializer.Serialize(new
            {
                segmentIndex,
                userText = request.UserText,
                level = request.Level
            });
            var resultDetails = JsonSerializer.Serialize(new
            {
                segmentIndex,
                accuracy,
                wordResults
            });
            var newScore = (int)Math.Round(accuracy);

            // UPSERT: tránh duplicate record cho cùng user + lesson + segment
            var existing = await _dbContext.UserExercises
                .FirstOrDefaultAsync(
                    e => e.UserId == userId
                      && e.LessonId == lessonId
                      && e.SegmentIndex == segmentIndex
                      && e.ExerciseType == ExerciseType.Dictation,
                    cancellationToken);

            if (existing is not null)
            {
                existing.Attempts++;
                existing.Score = Math.Max(existing.Score, newScore); // giữ best score
                existing.UserInput = userInput;
                existing.ResultDetails = resultDetails;
                existing.TimeSpentSeconds = request.TimeSpentSeconds;
                existing.CompletedAt = DateTime.UtcNow;
                _dbContext.UserExercises.Update(existing);
            }
            else
            {
                var exercise = new UserExercise
                {
                    UserId = userId,
                    LessonId = lessonId,
                    ExerciseType = ExerciseType.Dictation,
                    SegmentIndex = segmentIndex,
                    UserInput = userInput,
                    ResultDetails = resultDetails,
                    Score = newScore,
                    TimeSpentSeconds = request.TimeSpentSeconds,
                    CompletedAt = DateTime.UtcNow
                };
                _dbContext.UserExercises.Add(exercise);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Không fail toàn bộ request vì lỗi lưu exercise
            _logger.LogWarning(ex, "Failed to save UserExercise for segment check (lesson {LessonId}, segment {Index})",
                lessonId, segmentIndex);
        }
    }
}
