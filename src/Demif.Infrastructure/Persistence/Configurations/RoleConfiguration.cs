using Demif.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Demif.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration cho Role entity
/// </summary>
public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.Property(r => r.Permissions)
            .HasColumnType("text"); // JSON data

        // Index
        builder.HasIndex(r => r.Name).IsUnique();

        // Seed default roles
        builder.HasData(
            new Role
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Name = "Admin",
                Description = "Quản trị viên hệ thống - full quyền",
                IsDefault = false,
                IsActive = true,
                Permissions = "{\"canManageUsers\": true, \"canManageContent\": true, \"canViewReports\": true, \"canManagePayments\": true}",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Role
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Name = "User",
                Description = "Người dùng thông thường",
                IsDefault = true,
                IsActive = true,
                Permissions = "{\"canAccessLessons\": true, \"canSubmitExercises\": true}",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Role
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Premium",
                Description = "Người dùng Premium - không giới hạn bài học",
                IsDefault = false,
                IsActive = true,
                Permissions = "{\"canAccessLessons\": true, \"canSubmitExercises\": true, \"canAccessPremiumContent\": true, \"unlimitedLessons\": true, \"aiFeatures\": true}",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new Role
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Name = "Moderator",
                Description = "Điều phối viên - quản lý nội dung",
                IsDefault = false,
                IsActive = true,
                Permissions = "{\"canManageContent\": true, \"canViewReports\": true}",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
    }
}
