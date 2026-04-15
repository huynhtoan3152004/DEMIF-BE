using System;

namespace Demif.Application.Features.Blogs.GetBlogs
{
    public class BlogDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? Tags { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime? PublishedAt { get; set; }
        public int ReadingTimeMinutes { get; set; }
        public bool IsFeatured { get; set; }
        public int ViewCount { get; set; }
        public Guid AuthorId { get; set; }
        public string? AuthorName { get; set; }
        public string? AuthorAvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}