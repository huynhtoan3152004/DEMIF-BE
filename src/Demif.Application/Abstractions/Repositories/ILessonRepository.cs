using Demif.Domain.Entities;
using Demif.Domain.Enums;

namespace Demif.Application.Abstractions.Repositories;

/// <summary>
/// Lesson Repository interface
/// </summary>
public interface ILessonRepository : IGenericRepository<Lesson>
{
    Task<IEnumerable<Lesson>> GetByLevelAsync(Level level, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lesson>> GetByTypeAsync(LessonType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lesson>> GetPublishedAsync(CancellationToken cancellationToken = default);
}
