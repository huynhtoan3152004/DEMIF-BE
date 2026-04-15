using Demif.Application.Abstractions.Repositories;
using Demif.Application.Abstractions.Services;
using Demif.Application.Features.Blogs.CreateBlog;
using Demif.Application.Features.Blogs.DeleteBlog;
using Demif.Application.Features.Blogs.GetBlogBySlug;
using Demif.Application.Features.Blogs.GetBlogs;
using Demif.Application.Features.Blogs.UpdateBlog;
using Demif.Domain.Entities;

namespace Demif.Tests.Blogs;

public class BlogFeatureTests
{
    [Fact]
    public async Task CreateBlogService_GeneratesUniqueSlugAndMetadata()
    {
        var repository = new FakeBlogRepository();
        repository.ExistingSlugs.Add("my-first-post");
        var currentUser = new FakeCurrentUserService(Guid.NewGuid());
        var imageUploadService = new FakeImageUploadService();
        var service = new CreateBlogService(repository, currentUser, imageUploadService);

        var request = new CreateBlogRequest
        {
            Title = "My First Post",
            Content = string.Join(' ', Enumerable.Repeat("word", 220)),
            Category = "tips",
            IsFeatured = true,
            Status = "published"
        };

        var blogId = await service.ExecuteAsync(request);

        var created = repository.AddedBlogs.Single(x => x.Id == blogId);
        Assert.Equal("my-first-post-2", created.Slug);
        Assert.Equal("tips", created.Category);
        Assert.True(created.PublishedAt.HasValue);
        Assert.Equal(2, created.ReadingTimeMinutes);
        Assert.True(created.IsFeatured);
        Assert.False(created.IsDeleted);
    }

    [Fact]
    public async Task GetBlogBySlugService_IncrementsViewCount()
    {
        var blog = new Blog
        {
            Id = Guid.NewGuid(),
            Title = "SEO Friendly Title",
            Slug = "seo-friendly-title",
            Content = "Content",
            Status = "published",
            ViewCount = 3,
            AuthorId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var repository = new FakeBlogRepository();
        repository.Blogs.Add(blog);

        var service = new GetBlogBySlugService(repository);
        var result = await service.ExecuteAsync("seo-friendly-title");

        Assert.NotNull(result);
        Assert.Equal(4, result!.ViewCount);
        Assert.Equal(4, repository.Blogs.Single().ViewCount);
        Assert.Equal("seo-friendly-title", result.Slug);
    }

    [Fact]
    public async Task DeleteBlogService_SoftDeletesAndArchives()
    {
        var blog = new Blog
        {
            Id = Guid.NewGuid(),
            Title = "Archive Me",
            Slug = "archive-me",
            Content = "Content",
            Status = "published",
            ViewCount = 0,
            AuthorId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var repository = new FakeBlogRepository();
        repository.Blogs.Add(blog);

        var service = new DeleteBlogService(repository);
        var result = await service.ExecuteAsync(blog.Id);

        Assert.True(result);
        Assert.True(blog.IsDeleted);
        Assert.Equal("archived", blog.Status);
        Assert.NotNull(blog.DeletedAt);
        Assert.Single(repository.Blogs);
    }

    private sealed class FakeBlogRepository : IBlogRepository
    {
        public List<Blog> Blogs { get; } = new();
        public HashSet<string> ExistingSlugs { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<Blog> AddedBlogs { get; } = new();

        public Task<Blog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Blogs.FirstOrDefault(x => x.Id == id));

        public Task<IEnumerable<Blog>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<Blog>>(Blogs.ToList());

        public Task<Blog> AddAsync(Blog entity, CancellationToken cancellationToken = default)
        {
            Blogs.Add(entity);
            AddedBlogs.Add(entity);
            ExistingSlugs.Add(entity.Slug);
            return Task.FromResult(entity);
        }

        public Task UpdateAsync(Blog entity, CancellationToken cancellationToken = default)
        {
            var index = Blogs.FindIndex(x => x.Id == entity.Id);
            if (index >= 0)
            {
                Blogs[index] = entity;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Blog entity, CancellationToken cancellationToken = default)
        {
            Blogs.RemoveAll(x => x.Id == entity.Id);
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<Blog> Items, int TotalCount)> GetPagedAsync(
            string? search,
            string? category,
            string? tag,
            string? status,
            bool includeDeleted,
            string sortBy,
            string sortDirection,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(((IReadOnlyList<Blog>)Blogs.ToList(), Blogs.Count));
        }

        public Task<Blog?> GetBySlugAsync(string slug, bool includeDeleted = false, CancellationToken cancellationToken = default)
            => Task.FromResult(Blogs.FirstOrDefault(x => x.Slug == slug));

        public Task<Blog?> GetByIdWithAuthorAsync(Guid id, bool includeDeleted = false, CancellationToken cancellationToken = default)
            => Task.FromResult(Blogs.FirstOrDefault(x => x.Id == id));

        public Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default)
        {
            var exists = ExistingSlugs.Contains(slug) || Blogs.Any(x => x.Slug == slug && (!excludeId.HasValue || x.Id != excludeId.Value));
            return Task.FromResult(exists);
        }
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public FakeCurrentUserService(Guid userId)
        {
            UserId = userId;
        }

        public Guid? UserId { get; }
        public string? Email => "admin@example.com";
        public bool IsAuthenticated => true;
    }

    private sealed class FakeImageUploadService : IImageUploadService
    {
        public Task<string?> UploadImageAsync(Microsoft.AspNetCore.Http.IFormFile file, string folderName)
            => Task.FromResult<string?>(null);
    }
}
