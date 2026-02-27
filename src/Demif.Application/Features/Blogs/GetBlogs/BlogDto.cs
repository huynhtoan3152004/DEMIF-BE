using System;

namespace Demif.Application.Features.Blogs.GetBlogs
{
    public class BlogDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Tags { get; set; }
        public string Status { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public Guid AuthorId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}