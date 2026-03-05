using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;

namespace Demif.Application.Features.Me.GetStreak;

/// <summary>
/// Service lấy thông tin chuỗi ngày học liên tiếp của user
/// </summary>
public class GetStreakService
{
    private readonly IUserStreakRepository _streakRepository;

    public GetStreakService(IUserStreakRepository streakRepository)
    {
        _streakRepository = streakRepository;
    }

    public async Task<Result<GetStreakResponse>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var streak = await _streakRepository.GetByUserIdAsync(userId, cancellationToken);

        if (streak is null)
        {
            return Result.Success(new GetStreakResponse
            {
                CurrentStreak = 0,
                LongestStreak = 0,
                TotalActiveDays = 0,
                FreezesAvailable = 1,
                LastActiveDate = null,
                LearnedToday = false
            });
        }

        var today = DateTime.UtcNow.Date;
        var learnedToday = streak.LastActiveDate.HasValue &&
                           streak.LastActiveDate.Value.Date == today;

        return Result.Success(new GetStreakResponse
        {
            CurrentStreak = streak.CurrentStreak,
            LongestStreak = streak.LongestStreak,
            TotalActiveDays = streak.TotalActiveDays,
            FreezesAvailable = streak.FreezesAvailable,
            LastActiveDate = streak.LastActiveDate,
            LearnedToday = learnedToday
        });
    }
}
