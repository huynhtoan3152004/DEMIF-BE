using System;
using System.Threading;
using System.Threading.Tasks;
using Demif.Domain.Entities;

namespace Demif.Application.Features.Me.GetUserAnalytics
{
    public class UserAnalyticsResponse
    {
        public int TotalExercisesCompleted { get; set; }
        public int TotalLessonsCompleted { get; set; }
        public int TotalLearningMinutes { get; set; }
        public int TotalPoints { get; set; }
        public decimal AvgDictationScore { get; set; }
        public decimal AvgShadowingScore { get; set; }
        public int HighestScore { get; set; }
        public int PerfectScoresCount { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestStreak { get; set; }
        public int TotalActiveDays { get; set; }
        public string? SkillsBreakdown { get; set; }
        public string? LessonTypeStats { get; set; }
        public string? LevelStats { get; set; }
        public string? CategoryStats { get; set; }
        public string? TopLessons { get; set; }
        public string? RecentLessons { get; set; }
        public string? WeeklyTrends { get; set; }
        public string? MonthlyTrends { get; set; }
        public decimal WeeklyImprovement { get; set; }
        public decimal MonthlyImprovement { get; set; }
        public int EngagementScore { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
