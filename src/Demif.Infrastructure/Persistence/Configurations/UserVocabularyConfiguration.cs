using Demif.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Demif.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for saved vocabulary items.
/// </summary>
public class UserVocabularyConfiguration : IEntityTypeConfiguration<UserVocabulary>
{
    public void Configure(EntityTypeBuilder<UserVocabulary> builder)
    {
        builder.ToTable("UserVocabularies");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Topic)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(x => x.Word)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.NormalizedWord)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Meaning)
            .HasMaxLength(500);

        builder.Property(x => x.ContextSentence)
            .HasColumnType("text");

        builder.Property(x => x.Note)
            .HasColumnType("text");

        builder.Property(x => x.ReviewCount)
            .HasDefaultValue(0);

        builder.Property(x => x.CorrectReviews)
            .HasDefaultValue(0);

        builder.Property(x => x.IsMastered)
            .HasDefaultValue(false);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.LessonId);
        builder.HasIndex(x => x.Topic);
        builder.HasIndex(x => x.NextReviewAt);
        builder.HasIndex(x => new { x.UserId, x.LessonId, x.Topic, x.NormalizedWord })
            .IsUnique();

        builder.HasOne(x => x.User)
            .WithMany(u => u.Vocabularies)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Lesson)
            .WithMany(l => l.Vocabularies)
            .HasForeignKey(x => x.LessonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}