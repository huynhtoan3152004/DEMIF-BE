using Demif.Domain.Common;

namespace Demif.Domain.Entities;

public class Post : AuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public Guid AuthorId { get; set; }
    public virtual User Author { get; set; } = null!;
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public virtual ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
}