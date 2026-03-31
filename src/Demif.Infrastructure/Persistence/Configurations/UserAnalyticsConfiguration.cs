using Demif.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Demif.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration cho UserAnalytics entity
/// </summary>
public class UserAnalyticsConfiguration : IEntityTypeConfiguration<UserAnalytics>
{
    public void Configure(EntityTypeBuilder<UserAnalytics> builder)
    {
        builder.ToTable("UserAnalytics");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        // Learning Statistics
        builder.Property(x => x.TotalExercisesCompleted)
            .HasDefaultValue(0);

        builder.Property(x => x.TotalLessonsCompleted)
            .HasDefaultValue(0);

        builder.Property(x => x.TotalLearningMinutes)
            .HasDefaultValue(0);

        builder.Property(x => x.TotalPoints)
            .HasDefaultValue(0);

        builder.Property(x => x.AvgDictationScore)
            .HasPrecision(5, 2)
            .HasDefaultValue(0);

        builder.Property(x => x.AvgShadowingScore)
            .HasPrecision(5, 2)
            .HasDefaultValue(0);

        builder.Property(x => x.HighestScore)
            .HasDefaultValue(0);

        builder.Property(x => x.PerfectScoresCount)
            .HasDefaultValue(0);

        // Activity Patterns
        builder.Property(x => x.TotalActiveDays)
            .HasDefaultValue(0);

        builder.Property(x => x.CurrentStreak)
            .HasDefaultValue(0);

        builder.Property(x => x.LongestStreak)
            .HasDefaultValue(0);

        builder.Property(x => x.StreakFreezesUsed)
            .HasDefaultValue(0);

        builder.Property(x => x.AvgSessionsPerWeek)
            .HasPrecision(5, 2)
            .HasDefaultValue(0);

        // Performance Trends
        builder.Property(x => x.WeeklyImprovement)
            .HasPrecision(5, 2)
            .HasDefaultValue(0);

        builder.Property(x => x.MonthlyImprovement)
            .HasPrecision(5, 2)
            .HasDefaultValue(0);

        // Subscription & Payment
        builder.Property(x => x.TotalAmountPaid)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(x => x.SuccessfulPaymentsCount)
            .HasDefaultValue(0);

        // Engagement Metrics
        builder.Property(x => x.TotalLogins)
            .HasDefaultValue(0);

        builder.Property(x => x.BlogViewsCount)
            .HasDefaultValue(0);

        builder.Property(x => x.EngagementScore)
            .HasDefaultValue(0);

        // Metadata
        builder.Property(x => x.SchemaVersion)
            .HasDefaultValue(1);

        builder.Property(x => x.UpdatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // JSON columns - store as jsonb
        builder.Property(x => x.LessonTypeStats)
            .HasColumnType("jsonb");

        builder.Property(x => x.LevelStats)
            .HasColumnType("jsonb");

        builder.Property(x => x.CategoryStats)
            .HasColumnType("jsonb");

        builder.Property(x => x.TopLessons)
            .HasColumnType("jsonb");

        builder.Property(x => x.RecentLessons)
            .HasColumnType("jsonb");

        builder.Property(x => x.WeeklyTrends)
            .HasColumnType("jsonb");

        builder.Property(x => x.MonthlyTrends)
            .HasColumnType("jsonb");

        builder.Property(x => x.SkillsBreakdown)
            .HasColumnType("jsonb");

        // Index for fast lookup by UserId
        builder.HasIndex(x => x.UserId)
            .IsUnique();

        // Relationship with User
        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<UserAnalytics>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
