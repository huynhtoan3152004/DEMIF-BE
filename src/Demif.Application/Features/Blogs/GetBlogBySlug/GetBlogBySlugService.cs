using System.Threading.Tasks;
using Demif.Application.Abstractions.Repositories;
using Demif.Application.Features.Blogs.GetBlogs;

namespace Demif.Application.Features.Blogs.GetBlogBySlug;

public interface IGetBlogBySlugService
{
    Task<BlogDto?> ExecuteAsync(string slug);
}

public class GetBlogBySlugService : IGetBlogBySlugService
{
    private readonly IBlogRepository _blogRepository;

    public GetBlogBySlugService(IBlogRepository blogRepository)
    {
        _blogRepository = blogRepository;
    }

    public async Task<BlogDto?> ExecuteAsync(string slug)
    {
        var blog = await _blogRepository.GetBySlugAsync(slug);
        if (blog is null)
        {
            return null;
        }

        blog.ViewCount += 1;
        blog.UpdatedAt = DateTime.UtcNow;
        await _blogRepository.UpdateAsync(blog);

        return new BlogDto
        {
            Id = blog.Id,
            Title = blog.Title,
            Slug = blog.Slug,
            Category = blog.Category,
            Content = blog.Content,
            Summary = blog.Summary,
            ThumbnailUrl = blog.ThumbnailUrl,
            Tags = blog.Tags,
            Status = blog.Status,
            PublishedAt = blog.PublishedAt,
            ReadingTimeMinutes = blog.ReadingTimeMinutes,
            IsFeatured = blog.IsFeatured,
            ViewCount = blog.ViewCount,
            AuthorId = blog.AuthorId,
            AuthorName = blog.Author?.Username,
            AuthorAvatarUrl = blog.Author?.AvatarUrl,
            CreatedAt = blog.CreatedAt,
            UpdatedAt = blog.UpdatedAt
        };
    }
}
