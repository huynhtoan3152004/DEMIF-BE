using Demif.Application.Common.Models;
using Demif.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Demif.Application.Abstractions.Persistence;

namespace Demif.Application.Features.Me.Stats;

/// <summary>
/// Service lấy Bảng Xếp Hạng người dùng dựa vào Số ngày Streak cao nhất hiện hành, sau đó tới Tổng Điểm
/// </summary>
public class GetLeaderboardService
{
    private readonly IApplicationDbContext _dbContext;

    public GetLeaderboardService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<List<LeaderboardItemResponse>>> ExecuteAsync(int limit = 10, CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users
            .Where(u => u.Status == Demif.Domain.Enums.UserStatus.Active)
            .Include(u => u.Streak)
            .Include(u => u.Progress)
            .OrderByDescending(u => u.Streak != null ? u.Streak.CurrentStreak : 0)
            .ThenByDescending(u => u.Progress != null ? u.Progress.TotalPoints : 0)
            .Take(limit)
            .Select(u => new LeaderboardItemResponse
            {
                UserId = u.Id,
                Username = u.Username,
                AvatarUrl = u.AvatarUrl,
                CurrentStreak = u.Streak != null ? u.Streak.CurrentStreak : 0,
                TotalPoints = u.Progress != null ? u.Progress.TotalPoints : 0,
                Level = u.Progress != null ? u.Progress.CurrentLevel.ToString() : "Beginner"
            })
            .ToListAsync(cancellationToken);

        return Result.Success(users);
    }
}

public class LeaderboardItemResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int CurrentStreak { get; set; }
    public int TotalPoints { get; set; }
    public string Level { get; set; } = string.Empty;
}
