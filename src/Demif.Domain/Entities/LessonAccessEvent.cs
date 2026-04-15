using Demif.Domain.Common;

namespace Demif.Domain.Entities;

public class LessonAccessEvent : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid LessonId { get; set; }
    public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
    public string AccessType { get; set; } = "detail";

    public virtual User? User { get; set; }
    public virtual Lesson? Lesson { get; set; }
}