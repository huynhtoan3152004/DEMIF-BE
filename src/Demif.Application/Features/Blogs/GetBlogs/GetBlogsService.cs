using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Demif.Application.Abstractions.Repositories;

namespace Demif.Application.Features.Blogs.GetBlogs
{
    public interface IGetBlogsService
    {
        Task<IEnumerable<BlogDto>> ExecuteAsync();
    }

    public class GetBlogsService : IGetBlogsService
    {
        private readonly IBlogRepository _blogRepository;

        public GetBlogsService(IBlogRepository blogRepository)
        {
            _blogRepository = blogRepository;
        }

        public async Task<IEnumerable<BlogDto>> ExecuteAsync()
        {
            var blogs = await _blogRepository.GetAllAsync();

            return blogs.Select(b => new BlogDto
            {
                Id = b.Id,
                Title = b.Title,
                Content = b.Content,
                Summary = b.Summary,
                ThumbnailUrl = b.ThumbnailUrl,
                Tags = b.Tags,
                Status = b.Status,
                ViewCount = b.ViewCount,
                AuthorId = b.AuthorId,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt
            }).OrderByDescending(b => b.CreatedAt).ToList();
        }
    }
}