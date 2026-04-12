using Demif.Domain.Common;
using Demif.Domain.Enums;

namespace Demif.Domain.Entities;

/// <summary>
/// Entity UserLessonTracker - Theo dõi tiến độ của từng người dùng trong từng bài học
/// </summary>
public class UserLessonTracker : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid LessonId { get; set; }

    public LessonProgressStatus Status { get; set; } = LessonProgressStatus.Started;

    /// <summary>
    /// Vị trí segment cuối cùng người dùng nghe/đang làm
    /// </summary>
    public int LastSegmentIndex { get; set; }

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public virtual User? User { get; set; }
    public virtual Lesson? Lesson { get; set; }
}
