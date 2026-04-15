using System;
using System.Threading.Tasks;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Features.Blogs;
using Demif.Domain.Entities;

namespace Demif.Application.Features.Blogs.CreateBlog
{
    public interface ICreateBlogService
    {
        Task<Guid> ExecuteAsync(CreateBlogRequest request);
    }

    public class CreateBlogService : ICreateBlogService
    {
        private readonly IBlogRepository _blogRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IImageUploadService _imageUploadService;

        public CreateBlogService(
            IBlogRepository blogRepository,
            ICurrentUserService currentUserService,
            IImageUploadService imageUploadService)
        {
            _blogRepository = blogRepository;
            _currentUserService = currentUserService;
            _imageUploadService = imageUploadService;
        }

        public async Task<Guid> ExecuteAsync(CreateBlogRequest request)
        {
            var adminId = _currentUserService.UserId;
            if (adminId == null || adminId == Guid.Empty)
            {
                throw new Exception("Không xác định được danh tính Admin.");
            }

            // Gọi Cloudinary upload ảnh
            string? uploadedImageUrl = null;
            if (request.ThumbnailFile != null)
            {
                uploadedImageUrl = await _imageUploadService.UploadImageAsync(request.ThumbnailFile, "demif-blogs");
            }

            var slug = string.IsNullOrWhiteSpace(request.Slug)
                ? BlogUtilities.CreateSlug(request.Title)
                : BlogUtilities.CreateSlug(request.Slug);

            slug = await EnsureUniqueSlugAsync(slug);

            DateTime? publishedAt = string.Equals(request.Status, "published", StringComparison.OrdinalIgnoreCase)
                ? DateTime.UtcNow
                : null;

            var blog = new Blog
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Slug = slug,
                Category = request.Category,
                Content = request.Content,
                Summary = request.Summary,
                ThumbnailUrl = uploadedImageUrl, // Gán URL lấy từ Cloudinary
                Tags = request.Tags,
                Status = request.Status ?? "published",
                PublishedAt = publishedAt,
                ReadingTimeMinutes = BlogUtilities.EstimateReadingTimeMinutes(request.Content),
                IsFeatured = request.IsFeatured,
                IsDeleted = false,
                AuthorId = adminId.Value,
                ViewCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _blogRepository.AddAsync(blog);
            return blog.Id;
        }

        private async Task<string> EnsureUniqueSlugAsync(string slug)
        {
            var uniqueSlug = string.IsNullOrWhiteSpace(slug) ? "blog" : slug;
            var suffix = 1;

            while (await _blogRepository.SlugExistsAsync(uniqueSlug))
            {
                suffix++;
                uniqueSlug = $"{slug}-{suffix}";
            }

            return uniqueSlug;
        }
    }
}