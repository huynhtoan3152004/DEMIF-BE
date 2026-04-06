using Demif.Application.Abstractions.Persistence;
using Demif.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Demif.Application.Features.Me.Stats;

public class GetActivityHeatmapService
{
    private readonly IApplicationDbContext _dbContext;

    public GetActivityHeatmapService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ActivityHeatmapResponse>> ExecuteAsync(
        Guid userId,
        int months = 6,
        CancellationToken ct = default)
    {
        months = Math.Clamp(months, 1, 12);

        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddMonths(-months);

        var dailyCounts = await _dbContext.UserExercises
            .Where(e => e.UserId == userId && e.CompletedAt.Date >= startDate && e.CompletedAt.Date <= endDate)
            .GroupBy(e => e.CompletedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Date, x => x.Count, ct);

        var data = new List<HeatmapDay>();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            data.Add(new HeatmapDay
            {
                Date = date.ToString("yyyy-MM-dd"),
                Count = dailyCounts.TryGetValue(date, out var count) ? count : 0
            });
        }

        return Result.Success(new ActivityHeatmapResponse
        {
            StartDate = startDate.ToString("yyyy-MM-dd"),
            EndDate = endDate.ToString("yyyy-MM-dd"),
            TotalActivities = dailyCounts.Values.Sum(),
            Data = data
        });
    }
}

public class ActivityHeatmapResponse
{
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public int TotalActivities { get; set; }
    public List<HeatmapDay> Data { get; set; } = new();
}

public class HeatmapDay
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}
