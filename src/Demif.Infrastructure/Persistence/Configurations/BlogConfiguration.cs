using Demif.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Demif.Infrastructure.Persistence.Configurations;

public class BlogConfiguration : IEntityTypeConfiguration<Blog>
{
    public void Configure(EntityTypeBuilder<Blog> builder)
    {
        builder.ToTable("Blogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(x => x.Slug)
            .IsUnique();

        builder.Property(x => x.Category)
            .HasMaxLength(80);

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.Summary)
            .HasMaxLength(500);

        builder.Property(x => x.ThumbnailUrl)
            .HasMaxLength(500);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("published");

        builder.Property(x => x.PublishedAt);
        builder.Property(x => x.ReadingTimeMinutes).HasDefaultValue(1);
        builder.Property(x => x.IsFeatured).HasDefaultValue(false);
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);
        builder.Property(x => x.ViewCount).HasDefaultValue(0);

        builder.Property(x => x.Tags)
            .HasColumnType("text");

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.PublishedAt);
        builder.HasIndex(x => new { x.IsDeleted, x.Status, x.PublishedAt });

        builder.HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
