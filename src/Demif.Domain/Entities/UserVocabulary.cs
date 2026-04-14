using Demif.Domain.Common;

namespace Demif.Domain.Entities;

/// <summary>
/// Saved vocabulary item for a user, grouped by lesson and topic.
/// </summary>
public class UserVocabulary : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid LessonId { get; set; }

    public string Topic { get; set; } = string.Empty;
    public string Word { get; set; } = string.Empty;
    public string NormalizedWord { get; set; } = string.Empty;
    public string? Meaning { get; set; }
    public string? ContextSentence { get; set; }
    public string? Note { get; set; }

    public int ReviewCount { get; set; }
    public int CorrectReviews { get; set; }
    public int ConsecutiveCorrect { get; set; }
    public bool IsMastered { get; set; }
    public DateTime? LastReviewedAt { get; set; }
    public DateTime? NextReviewAt { get; set; }
    public DateTime? MasteredAt { get; set; }

    public virtual User? User { get; set; }
    public virtual Lesson? Lesson { get; set; }
}