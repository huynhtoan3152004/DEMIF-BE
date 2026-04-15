using System;
using System.Threading.Tasks;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Features.Blogs;

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
            var blog = await _blogRepository.GetByIdWithAuthorAsync(id, includeDeleted: true);
            if (blog == null) return false;

            blog.Status = "archived";
            blog.IsDeleted = true;
            blog.DeletedAt = DateTime.UtcNow;
            blog.UpdatedAt = DateTime.UtcNow;

            await _blogRepository.UpdateAsync(blog);
            return true;
        }
    }
}