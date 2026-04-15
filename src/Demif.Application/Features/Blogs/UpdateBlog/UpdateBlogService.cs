using System;
using System.Threading.Tasks;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Features.Blogs;

namespace Demif.Application.Features.Blogs.UpdateBlog
{
    public interface IUpdateBlogService
    {
        Task<bool> ExecuteAsync(Guid id, UpdateBlogRequest request);
    }

    public class UpdateBlogService : IUpdateBlogService
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IImageUploadService _imageUploadService;

        public UpdateBlogService(IBlogRepository blogRepository, IImageUploadService imageUploadService)
        {
            _blogRepository = blogRepository;
            _imageUploadService = imageUploadService;
        }

        public async Task<bool> ExecuteAsync(Guid id, UpdateBlogRequest request)
        {
            var blog = await _blogRepository.GetByIdWithAuthorAsync(id, includeDeleted: true);
            if (blog == null) return false;

            if (request.ThumbnailFile != null)
            {
                var uploadedUrl = await _imageUploadService.UploadImageAsync(request.ThumbnailFile, "demif-blogs");
                if (!string.IsNullOrEmpty(uploadedUrl))
                {
                    blog.ThumbnailUrl = uploadedUrl;
                }
            }

            if (!string.IsNullOrWhiteSpace(request.Slug))
            {
                var requestedSlug = BlogUtilities.CreateSlug(request.Slug);
                if (!string.Equals(requestedSlug, blog.Slug, StringComparison.OrdinalIgnoreCase))
                {
                    blog.Slug = await EnsureUniqueSlugAsync(requestedSlug, blog.Id);
                }
            }

            blog.Title = request.Title;
            blog.Category = request.Category;
            blog.Content = request.Content;
            blog.Summary = request.Summary;
            blog.Tags = request.Tags;
            blog.Status = request.Status;
            blog.ReadingTimeMinutes = BlogUtilities.EstimateReadingTimeMinutes(request.Content);
            blog.IsFeatured = request.IsFeatured;
            blog.PublishedAt = string.Equals(request.Status, "published", StringComparison.OrdinalIgnoreCase)
                ? blog.PublishedAt ?? DateTime.UtcNow
                : blog.PublishedAt;
            blog.UpdatedAt = DateTime.UtcNow;

            await _blogRepository.UpdateAsync(blog);
            return true;
        }

        private async Task<string> EnsureUniqueSlugAsync(string slug, Guid excludeId)
        {
            var uniqueSlug = string.IsNullOrWhiteSpace(slug) ? "blog" : slug;
            var suffix = 1;

            while (await _blogRepository.SlugExistsAsync(uniqueSlug, excludeId))
            {
                suffix++;
                uniqueSlug = $"{slug}-{suffix}";
            }

            return uniqueSlug;
        }
    }
}