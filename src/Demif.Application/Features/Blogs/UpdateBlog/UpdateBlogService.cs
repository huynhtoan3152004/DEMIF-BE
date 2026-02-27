using System;
using System.Threading.Tasks;
using Demif.Application.Abstractions.Repositories;

namespace Demif.Application.Features.Blogs.UpdateBlog
{
    public interface IUpdateBlogService
    {
        Task<bool> ExecuteAsync(Guid id, UpdateBlogRequest request);
    }

    public class UpdateBlogService : IUpdateBlogService
    {
        private readonly IBlogRepository _blogRepository;

        public UpdateBlogService(IBlogRepository blogRepository)
        {
            _blogRepository = blogRepository;
        }

        public async Task<bool> ExecuteAsync(Guid id, UpdateBlogRequest request)
        {
            var blog = await _blogRepository.GetByIdAsync(id);
            if (blog == null) return false;

            // Cập nhật các trường dữ liệu
            blog.Title = request.Title;
            blog.Content = request.Content;
            blog.Summary = request.Summary;
            blog.ThumbnailUrl = request.ThumbnailUrl;
            blog.Tags = request.Tags;
            blog.Status = request.Status;
            blog.UpdatedAt = DateTime.UtcNow;

            await _blogRepository.UpdateAsync(blog);
            return true;
        }
    }
}