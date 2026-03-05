using Demif.Application.Abstractions.Repositories;
using Demif.Application.Common.Models;
using Demif.Domain.Enums;

namespace Demif.Application.Features.Me.GetProgress;

/// <summary>
/// Service lấy tiến độ học tập tổng quan của user
/// </summary>
public class GetProgressService
{
    private readonly IUserProgressRepository _progressRepository;

    public GetProgressService(IUserProgressRepository progressRepository)
    {
        _progressRepository = progressRepository;
    }

    public async Task<Result<GetProgressResponse>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var progress = await _progressRepository.GetByUserIdAsync(userId, cancellationToken);

        // Nếu chưa có record — trả về default (user mới)
        if (progress is null)
        {
            return Result.Success(new GetProgressResponse
            {
                TotalPoints = 0,
                TotalMinutes = 0,
                LessonsCompleted = 0,
                DictationCompleted = 0,
                ShadowingCompleted = 0,
                AvgDictationScore = 0,
                AvgShadowingScore = 0,
                CurrentLevel = Level.Beginner.ToString(),
                LevelProgress = 0
            });
        }

        return Result.Success(new GetProgressResponse
        {
            TotalPoints = progress.TotalPoints,
            TotalMinutes = progress.TotalMinutes,
            LessonsCompleted = progress.LessonsCompleted,
            DictationCompleted = progress.DictationCompleted,
            ShadowingCompleted = progress.ShadowingCompleted,
            AvgDictationScore = progress.AvgDictationScore,
            AvgShadowingScore = progress.AvgShadowingScore,
            CurrentLevel = progress.CurrentLevel.ToString(),
            LevelProgress = progress.LevelProgress
        });
    }
}
