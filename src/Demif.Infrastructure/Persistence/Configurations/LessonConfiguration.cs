using Demif.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Demif.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration cho Lesson entity
/// </summary>
public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("Lessons");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Title)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(l => l.AudioUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(l => l.ThumbnailUrl)
            .HasMaxLength(500);

        builder.Property(l => l.Category)
            .HasMaxLength(50);

        builder.Property(l => l.LessonType)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(l => l.Level)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(l => l.Status)
            .HasMaxLength(20)
            .HasDefaultValue("published");

        builder.Property(l => l.AvgScore)
            .HasPrecision(5, 2);

        // New fields for Dictation system
        builder.Property(l => l.TimedTranscript)
            .HasColumnType("text");

        builder.Property(l => l.DictationTemplates)
            .HasColumnType("text");

        builder.Property(l => l.MediaUrl)
            .HasMaxLength(500);

        builder.Property(l => l.MediaType)
            .HasMaxLength(20);

        builder.Property(l => l.FullTranscript)
            .HasColumnType("text");

        builder.Property(l => l.Tags)
            .HasColumnType("text");

        // Indexes
        builder.HasIndex(l => new { l.LessonType, l.Level });
        builder.HasIndex(l => l.Status);
        builder.HasIndex(l => l.Category);
    }
}
