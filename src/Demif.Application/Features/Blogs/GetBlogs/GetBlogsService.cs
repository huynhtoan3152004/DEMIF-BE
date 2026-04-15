using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Demif.Application.Abstractions.Repositories;

namespace Demif.Application.Features.Blogs.GetBlogs
{
    public interface IGetBlogsService
    {
        Task<PagedBlogResponse> ExecuteAsync(GetBlogsRequest request, bool includeDeleted = false);
    }

    public class GetBlogsService : IGetBlogsService
    {
        private readonly IBlogRepository _blogRepository;

        public GetBlogsService(IBlogRepository blogRepository)
        {
            _blogRepository = blogRepository;
        }

        public async Task<PagedBlogResponse> ExecuteAsync(GetBlogsRequest request, bool includeDeleted = false)
        {
            var page = Math.Max(1, request.Page);
            var pageSize = Math.Clamp(request.PageSize, 1, 50);
            var status = includeDeleted ? request.Status : "published";

            var (items, totalCount) = await _blogRepository.GetPagedAsync(
                request.Search,
                request.Category,
                request.Tag,
                status,
                includeDeleted || request.IncludeDeleted,
                request.SortBy,
                request.SortDirection,
                page,
                pageSize);

            var mappedItems = items.Select(MapToDto).ToList();
            return new PagedBlogResponse
            {
                Items = mappedItems,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        private static BlogDto MapToDto(Domain.Entities.Blog blog)
        {
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
}