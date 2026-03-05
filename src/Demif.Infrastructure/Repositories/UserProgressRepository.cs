using Demif.Application.Abstractions.Repositories;
using Demif.Domain.Entities;
using Demif.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Demif.Infrastructure.Repositories;

/// <summary>
/// UserProgress Repository implementation
/// </summary>
public class UserProgressRepository : IUserProgressRepository
{
    private readonly ApplicationDbContext _context;

    public UserProgressRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserProgress?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
    }

    public async Task<UserProgress> UpsertAsync(UserProgress progress, CancellationToken cancellationToken = default)
    {
        var existing = await _context.UserProgresses
            .FirstOrDefaultAsync(p => p.UserId == progress.UserId, cancellationToken);

        if (existing is null)
        {
            progress.Id = Guid.NewGuid();
            await _context.UserProgresses.AddAsync(progress, cancellationToken);
        }
        else
        {
            existing.TotalPoints = progress.TotalPoints;
            existing.TotalMinutes = progress.TotalMinutes;
            existing.LessonsCompleted = progress.LessonsCompleted;
            existing.DictationCompleted = progress.DictationCompleted;
            existing.ShadowingCompleted = progress.ShadowingCompleted;
            existing.AvgDictationScore = progress.AvgDictationScore;
            existing.AvgShadowingScore = progress.AvgShadowingScore;
            existing.CurrentLevel = progress.CurrentLevel;
            existing.LevelProgress = progress.LevelProgress;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return existing ?? progress;
    }
}
