using System;
using System.Threading.Tasks;
using Demif.Application.Abstractions.Repositories;

namespace Demif.Application.Features.Blogs.DeleteBlog
{
    public interface IDeleteBlogService
    {
        Task<bool> ExecuteAsync(Guid id);
    }

    public class DeleteBlogService : IDeleteBlogService
    {
        private readonly IBlogRepository _blogRepository;

        public DeleteBlogService(IBlogRepository blogRepository)
        {
            _blogRepository = blogRepository;
        }

        public async Task<bool> ExecuteAsync(Guid id)
        {
            var blog = await _blogRepository.GetByIdAsync(id);
            if (blog == null) return false;

            await _blogRepository.DeleteAsync(blog);
            return true;
        }
    }
}