using System;
using System.Threading.Tasks;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Domain.Entities;

namespace Demif.Application.Features.Blogs.CreateBlog
{
    // Đã thêm public
    public interface ICreateBlogService
    {
        Task<Guid> ExecuteAsync(CreateBlogRequest request);
    }

    // Đã thêm public
    public class CreateBlogService : ICreateBlogService
    {
        private readonly IBlogRepository _blogRepository;
        private readonly ICurrentUserService _currentUserService;

        public CreateBlogService(IBlogRepository blogRepository, ICurrentUserService currentUserService)
        {
            _blogRepository = blogRepository;
            _currentUserService = currentUserService;
        }

        public async Task<Guid> ExecuteAsync(CreateBlogRequest request)
        {
            // Lấy ID của Admin đang đăng nhập (từ JWT Token)
            var adminId = _currentUserService.UserId;

            if (adminId == Guid.Empty)
            {
                throw new Exception("Không xác định được danh tính Admin.");
            }

            var blog = new Blog
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                Content = request.Content,
                Summary = request.Summary,
                ThumbnailUrl = request.ThumbnailUrl,
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