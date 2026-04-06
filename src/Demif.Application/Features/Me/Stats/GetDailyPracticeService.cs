using Demif.Application.Abstractions.Persistence;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Demif.Application.Features.Me.Stats;

public class GetDailyPracticeService
{
    private const int BasePointsPerSession = 10;
    private const int PointsPerScorePercent = 1;

    private readonly IApplicationDbContext _dbContext;

    public GetDailyPracticeService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<DailyPracticeResponse>> ExecuteAsync(
        Guid userId,
        int days = 30,
        ExerciseType? exerciseType = null,
        CancellationToken ct = default)
    {
        days = Math.Clamp(days, 7, 90);

        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-(days - 1));

        var query = _dbContext.UserExercises
            .Where(e => e.UserId == userId && e.CompletedAt.Date >= startDate && e.CompletedAt.Date <= endDate);

        if (exerciseType.HasValue)
            query = query.Where(e => e.ExerciseType == exerciseType.Value);

        var dailyData = await query
            .GroupBy(e => e.CompletedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Minutes = g.Sum(e => (e.TimeSpentSeconds ?? 0) / 60),
                SessionsCount = g.Count(),
                TotalScore = g.Sum(e => e.Score)
            })
            .ToDictionaryAsync(x => x.Date, x => x, ct);

        var data = new List<DailyPracticeDay>();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (dailyData.TryGetValue(date, out var day))
            {
                var xpEarned = (day.SessionsCount * BasePointsPerSession) + (day.TotalScore * PointsPerScorePercent);
                data.Add(new DailyPracticeDay
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    Minutes = day.Minutes,
                    XpEarned = xpEarned,
                    SessionsCount = day.SessionsCount
                });
            }
            else
            {
                data.Add(new DailyPracticeDay
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    Minutes = 0,
                    XpEarned = 0,
                    SessionsCount = 0
                });
            }
        }

        return Result.Success(new DailyPracticeResponse
        {
            ExerciseType = exerciseType?.ToString() ?? "All",
            Days = days,
            Data = data
        });
    }
}

public class DailyPracticeResponse
{
    public string ExerciseType { get; set; } = "All";
    public int Days { get; set; }
    public List<DailyPracticeDay> Data { get; set; } = new();
}

public class DailyPracticeDay
{
    public string Date { get; set; } = string.Empty;
    public int Minutes { get; set; }
    public int XpEarned { get; set; }
    public int SessionsCount { get; set; }
}
