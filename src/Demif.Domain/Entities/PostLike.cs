using Demif.Domain.Common;

namespace Demif.Domain.Entities;

public class PostLike : BaseEntity
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public DateTime LikedAt { get; set; } = DateTime.UtcNow;
}