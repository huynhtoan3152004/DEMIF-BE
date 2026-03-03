using System;
using System.Threading.Tasks;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
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

            var blog = new Blog
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Content = request.Content,
                Summary = request.Summary,
                ThumbnailUrl = uploadedImageUrl, // Gán URL lấy từ Cloudinary
                Tags = request.Tags,
                Status = request.Status ?? "published",
                AuthorId = adminId.Value,
                ViewCount = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _blogRepository.AddAsync(blog);
            return blog.Id;
        }
    }
}