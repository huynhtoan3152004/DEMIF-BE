using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Demif.Application.Features.Blogs.UpdateBlog
{
    public class UpdateBlogRequest
    {
        [Required(ErrorMessage = "Tiêu đề không được để trống")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nội dung không được để trống")]
        public string Content { get; set; } = string.Empty;

        public string? Summary { get; set; }
        public IFormFile? ThumbnailFile { get; set; }
        public string? Tags { get; set; }
        public string Status { get; set; } = "published";
    }
}