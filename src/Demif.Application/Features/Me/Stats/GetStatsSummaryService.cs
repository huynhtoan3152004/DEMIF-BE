using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;

namespace Demif.Application.Features.Me.Stats;

public class GetStatsSummaryService
{
    private readonly IUserStreakRepository _streakRepo;
    private readonly IUserProgressRepository _progressRepo;

    public GetStatsSummaryService(
        IUserStreakRepository streakRepo,
        IUserProgressRepository progressRepo)
    {
        _streakRepo = streakRepo;
        _progressRepo = progressRepo;
    }

    public async Task<Result<StatsSummaryResponse>> ExecuteAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var streak = await _streakRepo.GetByUserIdAsync(userId, ct);
        var progress = await _progressRepo.GetByUserIdAsync(userId, ct);

        return Result.Success(new StatsSummaryResponse
        {
            LongestStreak = streak?.LongestStreak ?? 0,
            CurrentStreak = streak?.CurrentStreak ?? 0,
            SavedWordsCount = 0, // Placeholder — SavedWords feature chưa có
            TotalPracticeMinutes = progress?.TotalMinutes ?? 0,
            TotalXp = progress?.TotalPoints ?? 0
        });
    }
}

public class StatsSummaryResponse
{
    public int LongestStreak { get; set; }
    public int CurrentStreak { get; set; }
    public int SavedWordsCount { get; set; }
    public int TotalPracticeMinutes { get; set; }
    public int TotalXp { get; set; }
}
