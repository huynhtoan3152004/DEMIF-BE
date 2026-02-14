using Demif.Domain.Common;

namespace Demif.Domain.Entities;

public class Comment : AuditableEntity
{
    public string Content { get; set; } = string.Empty;
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;
}