using System;
using System.Threading.Tasks;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Features.Blogs.GetBlogs; 

namespace Demif.Application.Features.Blogs.GetBlogById
{
    public interface IGetBlogByIdService
    {
        Task<BlogDto?> ExecuteAsync(Guid id);
    }

    public class GetBlogByIdService : IGetBlogByIdService
    {
        private readonly IBlogRepository _blogRepository;

        public GetBlogByIdService(IBlogRepository blogRepository)
        {
            _blogRepository = blogRepository;
        }

        public async Task<BlogDto?> ExecuteAsync(Guid id)
        {
            var blog = await _blogRepository.GetByIdAsync(id);
            if (blog == null) return null;

            return new BlogDto
            {
                Id = blog.Id,
                Title = blog.Title,
                Content = blog.Content,
                Summary = blog.Summary,
                ThumbnailUrl = blog.ThumbnailUrl,
                Tags = blog.Tags,
                Status = blog.Status,
                ViewCount = blog.ViewCount,
                AuthorId = blog.AuthorId,
                CreatedAt = blog.CreatedAt,
                UpdatedAt = blog.UpdatedAt
            };
        }
    }
}