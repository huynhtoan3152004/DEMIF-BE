using Demif.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Demif.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration cho User entity
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.PasswordHash)
            .HasMaxLength(255);

        builder.Property(u => u.AvatarUrl)
            .HasMaxLength(500);

        builder.Property(u => u.Country)
            .HasMaxLength(100);

        builder.Property(u => u.NativeLanguage)
            .HasMaxLength(50)
            .HasDefaultValue("Vietnamese");

        builder.Property(u => u.TargetLanguage)
            .HasMaxLength(50)
            .HasDefaultValue("English");

        builder.Property(u => u.FirebaseUid)
            .HasMaxLength(128);

        builder.Property(u => u.AuthProvider)
            .HasMaxLength(30)
            .HasDefaultValue("email");

        // Indexes
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Username).IsUnique();
        builder.HasIndex(u => u.FirebaseUid).IsUnique();

        // Relationships
        builder.HasOne(u => u.Progress)
            .WithOne(p => p.User)
            .HasForeignKey<UserProgress>(p => p.UserId);

        builder.HasOne(u => u.Streak)
            .WithOne(s => s.User)
            .HasForeignKey<UserStreak>(s => s.UserId);
    }
}
