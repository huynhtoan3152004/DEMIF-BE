using Demif.Domain.Common;

namespace Demif.Domain.Entities;

/// <summary>
/// Entity Blog - Quản lý bài viết/tin tức
/// Kế thừa AuditableEntity để tự động có các trường CreatedAt, UpdatedAt...
/// </summary>
public class Blog : AuditableEntity
{
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    /// Mô tả ngắn hiển thị ở danh sách bài viết

    public string? Summary { get; set; }

    public string? ThumbnailUrl { get; set; }

    public string Status { get; set; } = "published";

    /// JSON array tags: ["tips", "grammar", "ielts"]
   
    public string? Tags { get; set; }

    public int ViewCount { get; set; }

    public Guid AuthorId { get; set; }

    // Navigation property
    public virtual User? Author { get; set; }
}