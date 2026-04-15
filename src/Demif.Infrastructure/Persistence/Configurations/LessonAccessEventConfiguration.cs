using Demif.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Demif.Infrastructure.Persistence.Configurations;

public class LessonAccessEventConfiguration : IEntityTypeConfiguration<LessonAccessEvent>
{
    public void Configure(EntityTypeBuilder<LessonAccessEvent> builder)
    {
        builder.ToTable("LessonAccessEvents");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.AccessType)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValue("detail");

        builder.Property(x => x.AccessedAt)
            .IsRequired();

        builder.HasIndex(x => new { x.LessonId, x.AccessedAt });
        builder.HasIndex(x => new { x.UserId, x.LessonId });
        builder.HasIndex(x => x.AccessType);
    }
}