using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Demif.Domain.Enums;

namespace Demif.Application.Features.Me.RecordActivity;

/// <summary>
/// Service ghi nhận kết thúc 1 phiên học:
///  - Cộng điểm vào UserProgress
///  - Cập nhật Streak (giữ/tăng/reset)
///  - Tính lại LevelProgress
/// </summary>
public class RecordActivityService
{
    // ── Cấu hình điểm ─────────────────────────────────────────────
    private const int BasePointsPerSession = 10;
    private const int PointsPerScorePercent = 1; // +1 point per % score

    // Thresholds điểm để level up (tổng cộng dồn)
    private static readonly Dictionary<Level, int> LevelThresholds = new()
    {
        { Level.Beginner,     0    },
        { Level.Intermediate, 500  },
        { Level.Advanced,     1500 },
        { Level.Expert,       3500 }
    };

    private readonly IUserProgressRepository _progressRepo;
    private readonly IUserStreakRepository _streakRepo;

    public RecordActivityService(
        IUserProgressRepository progressRepo,
        IUserStreakRepository streakRepo)
    {
        _progressRepo = progressRepo;
        _streakRepo = streakRepo;
    }

    public async Task<Result<RecordActivityResponse>> ExecuteAsync(
        Guid userId,
        RecordActivityRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Score < 0 || request.Score > 100)
            return Result.Failure<RecordActivityResponse>(Error.Validation("Score phải từ 0 đến 100."));

        // ── 1. Cập nhật Progress ──────────────────────────────────
        var progress = await _progressRepo.GetByUserIdAsync(userId, cancellationToken)
                       ?? new UserProgress { UserId = userId };

        var pointsEarned = BasePointsPerSession + (request.Score * PointsPerScorePercent);
        progress.TotalPoints += pointsEarned;
        progress.TotalMinutes += Math.Max(0, request.MinutesSpent);
        progress.LessonsCompleted++;

        if (request.ExerciseType == ExerciseType.Dictation)
        {
            progress.DictationCompleted++;
            // Rolling average
            progress.AvgDictationScore = progress.DictationCompleted == 1
                ? request.Score
                : Math.Round(
                    (progress.AvgDictationScore * (progress.DictationCompleted - 1) + request.Score)
                    / progress.DictationCompleted, 1);
        }
        else
        {
            progress.ShadowingCompleted++;
            progress.AvgShadowingScore = progress.ShadowingCompleted == 1
                ? request.Score
                : Math.Round(
                    (progress.AvgShadowingScore * (progress.ShadowingCompleted - 1) + request.Score)
                    / progress.ShadowingCompleted, 1);
        }

        // Tính lại level + level progress
        var (level, levelPct) = ComputeLevel(progress.TotalPoints);
        progress.CurrentLevel = level;
        progress.LevelProgress = levelPct;

        await _progressRepo.UpsertAsync(progress, cancellationToken);

        // ── 2. Cập nhật Streak ────────────────────────────────────
        var streak = await _streakRepo.GetByUserIdAsync(userId, cancellationToken)
                     ?? new UserStreak { UserId = userId, FreezesAvailable = 1 };

        var today = DateTime.UtcNow.Date;
        var streakIncreased = false;

        if (streak.LastActiveDate is null)
        {
            // Lần đầu học
            streak.CurrentStreak = 1;
            streak.LongestStreak = 1;
            streak.TotalActiveDays = 1;
            streakIncreased = true;
        }
        else
        {
            var lastDate = streak.LastActiveDate.Value.Date;

            if (lastDate == today)
            {
                // Đã học hôm nay — không thay đổi streak
            }
            else if (lastDate == today.AddDays(-1))
            {
                // Hôm qua đã học → tăng streak
                streak.CurrentStreak++;
                streak.TotalActiveDays++;
                streakIncreased = true;
                if (streak.CurrentStreak > streak.LongestStreak)
                    streak.LongestStreak = streak.CurrentStreak;
            }
            else
            {
                // Bị gián đoạn — reset
                streak.CurrentStreak = 1;
                streak.TotalActiveDays++;
                streakIncreased = true;
            }
        }

        streak.LastActiveDate = today;
        await _streakRepo.UpsertAsync(streak, cancellationToken);

        return Result.Success(new RecordActivityResponse
        {
            TotalPoints = progress.TotalPoints,
            PointsEarned = pointsEarned,
            CurrentStreak = streak.CurrentStreak,
            StreakIncreased = streakIncreased,
            CurrentLevel = progress.CurrentLevel.ToString(),
            LevelProgress = progress.LevelProgress
        });
    }

    // ── Helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Tính level hiện tại và % tiến độ đến level tiếp theo dựa vào tổng điểm
    /// </summary>
    private static (Level Level, int Pct) ComputeLevel(int totalPoints)
    {
        var orderedThresholds = LevelThresholds.OrderByDescending(kvp => kvp.Value).ToList();

        foreach (var (level, threshold) in orderedThresholds)
        {
            if (totalPoints >= threshold)
            {
                // Tìm ngưỡng next level
                var nextEntry = LevelThresholds
                    .Where(kvp => kvp.Value > threshold)
                    .OrderBy(kvp => kvp.Value)
                    .FirstOrDefault();

                if (nextEntry.Key == default)
                    return (level, 100); // Max level

                var range = nextEntry.Value - threshold;
                var earned = totalPoints - threshold;
                var pct = (int)Math.Min(100, Math.Round((double)earned / range * 100));
                return (level, pct);
            }
        }

        return (Level.Beginner, 0);
    }
}
