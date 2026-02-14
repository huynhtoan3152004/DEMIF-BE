using Demif.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Demif.Application.Abstractions.Persistence;

/// <summary>
/// Interface cho DbContext - Application không phụ thuộc vào EF Core implementation
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Lesson> Lessons { get; }
    DbSet<UserExercise> UserExercises { get; }
    DbSet<UserProgress> UserProgresses { get; }
    DbSet<UserStreak> UserStreaks { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<Post> Posts { get; }
    DbSet<Comment> Comments { get; }
    DbSet<PostLike> PostLikes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
