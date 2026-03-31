using System;
using System.Threading;
using System.Threading.Tasks;
using Demif.Application.Abstractions.Repositories;
using Demif.Domain.Entities;

namespace Demif.Application.Features.Me.GetUserAnalytics
{
    public class GetUserAnalyticsQueryHandler
    {
        private readonly GetUserAnalyticsService _service;

        public GetUserAnalyticsQueryHandler(GetUserAnalyticsService service)
        {
            _service = service;
        }

        public async Task<UserAnalyticsResponse?> Handle(Guid userId, CancellationToken cancellationToken)
        {
            var analytics = await _service.GetAnalyticsAsync(userId, cancellationToken);
            if (analytics == null) return null;
            return new UserAnalyticsResponse
            {
                TotalExercisesCompleted = analytics.TotalExercisesCompleted,
                TotalLessonsCompleted = analytics.TotalLessonsCompleted,
                TotalLearningMinutes = analytics.TotalLearningMinutes,
                TotalPoints = analytics.TotalPoints,
                AvgDictationScore = analytics.AvgDictationScore,
                AvgShadowingScore = analytics.AvgShadowingScore,
                HighestScore = analytics.HighestScore,
                PerfectScoresCount = analytics.PerfectScoresCount,
                CurrentStreak = analytics.CurrentStreak,
                LongestStreak = analytics.LongestStreak,
                TotalActiveDays = analytics.TotalActiveDays,
                SkillsBreakdown = analytics.SkillsBreakdown,
                LessonTypeStats = analytics.LessonTypeStats,
                LevelStats = analytics.LevelStats,
                CategoryStats = analytics.CategoryStats,
                TopLessons = analytics.TopLessons,
                RecentLessons = analytics.RecentLessons,
                WeeklyTrends = analytics.WeeklyTrends,
                MonthlyTrends = analytics.MonthlyTrends,
                WeeklyImprovement = analytics.WeeklyImprovement,
                MonthlyImprovement = analytics.MonthlyImprovement,
                EngagementScore = analytics.EngagementScore,
                UpdatedAt = analytics.UpdatedAt
            };
        }
    }
}
