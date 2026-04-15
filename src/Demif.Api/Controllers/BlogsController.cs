using Demif.Application.Features.Blogs.GetBlogById;
using Demif.Application.Features.Blogs.GetBlogBySlug;
using Demif.Application.Features.Blogs.GetBlogs;
using Demif.Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers;

/// <summary>
/// Public blog endpoints — read-only. Admin CRUD is at /api/admin/blogs.
/// </summary>
[ApiController]
[Route(ApiRoutes.Base + "/blogs")]
[Produces("application/json")]
[Tags("Blogs")]
public class BlogsController : ControllerBase
{
    private readonly IGetBlogsService _getBlogsService;
    private readonly IGetBlogByIdService _getBlogByIdService;
    private readonly IGetBlogBySlugService _getBlogBySlugService;

    public BlogsController(
        IGetBlogsService getBlogsService,
        IGetBlogByIdService getBlogByIdService,
        IGetBlogBySlugService getBlogBySlugService)
    {
        _getBlogsService = getBlogsService;
        _getBlogByIdService = getBlogByIdService;
        _getBlogBySlugService = getBlogBySlugService;
    }

    /// <summary>Get all published blog posts.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBlogs([FromQuery] GetBlogsRequest request)
    {
        request.Status = "published";
        request.IncludeDeleted = false;

        var blogs = await _getBlogsService.ExecuteAsync(request);
        return Ok(blogs);
    }

    /// <summary>Get a blog post by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBlogById(Guid id)
    {
        var blog = await _getBlogByIdService.ExecuteAsync(id);
        if (blog is null)
            return NotFound(new { message = "Không tìm thấy bài viết" });

        return Ok(blog);
    }

    /// <summary>Get a blog post by slug for SEO-friendly routes.</summary>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBlogBySlug(string slug)
    {
        var blog = await _getBlogBySlugService.ExecuteAsync(slug);
        if (blog is null)
            return NotFound(new { message = "Không tìm thấy bài viết" });

        return Ok(blog);
    }
}