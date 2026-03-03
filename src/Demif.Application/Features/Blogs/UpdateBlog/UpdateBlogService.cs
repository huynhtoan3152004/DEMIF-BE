using System;
using System.Threading.Tasks;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;

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
            var blog = await _blogRepository.GetByIdAsync(id);
            if (blog == null) return false;

            if (request.ThumbnailFile != null)
            {
                var uploadedUrl = await _imageUploadService.UploadImageAsync(request.ThumbnailFile, "demif-blogs");
                if (!string.IsNullOrEmpty(uploadedUrl))
                {
                    blog.ThumbnailUrl = uploadedUrl;
                }
            }

            blog.Title = request.Title;
            blog.Content = request.Content;
            blog.Summary = request.Summary;
            blog.Tags = request.Tags;
            blog.Status = request.Status;
            blog.UpdatedAt = DateTime.UtcNow;

            await _blogRepository.UpdateAsync(blog);
            return true;
        }
    }
}