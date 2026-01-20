using Demif.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Demif.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration cho UserExercise entity
/// </summary>
public class UserExerciseConfiguration : IEntityTypeConfiguration<UserExercise>
{
    public void Configure(EntityTypeBuilder<UserExercise> builder)
    {
        builder.ToTable("UserExercises");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.RecordingUrl)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.LessonId);
        builder.HasIndex(e => e.CompletedAt);
        builder.HasIndex(e => e.ExerciseType);

        // Relationships (soft references - không dùng FK trong DB)
        builder.HasOne(e => e.User)
            .WithMany(u => u.Exercises)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Lesson)
            .WithMany(l => l.Exercises)
            .HasForeignKey(e => e.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
