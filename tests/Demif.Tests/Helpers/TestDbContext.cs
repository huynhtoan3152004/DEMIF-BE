using Demif.Application.Abstractions.Persistence;
using Demif.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Demif.Tests.Helpers;

/// <summary>
/// In-memory DbContext for unit testing services that depend on IApplicationDbContext
/// </summary>
public class TestDbContext : DbContext, IApplicationDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<UserExercise> UserExercises => Set<UserExercise>();
    public DbSet<UserProgress> UserProgresses => Set<UserProgress>();
    public DbSet<UserStreak> UserStreaks => Set<UserStreak>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<UserAnalytics> UserAnalytics => Set<UserAnalytics>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // UserRole composite key
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });
    }
}
