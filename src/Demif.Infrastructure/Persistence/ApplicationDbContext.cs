using Demif.Application.Abstractions.Persistence;
using Demif.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Demif.Infrastructure.Persistence;

/// <summary>
/// DbContext chính - implement IApplicationDbContext
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<UserExercise> UserExercises => Set<UserExercise>();
    public DbSet<UserProgress> UserProgresses => Set<UserProgress>();
    public DbSet<UserStreak> UserStreaks => Set<UserStreak>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
