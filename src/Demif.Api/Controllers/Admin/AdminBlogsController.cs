using Demif.Application.Features.Blogs.CreateBlog;
using Demif.Application.Features.Blogs.DeleteBlog;
using Demif.Application.Features.Blogs.GetBlogs;
using Demif.Application.Features.Blogs.UpdateBlog;
using Demif.Api.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers.Admin;

/// <summary>
/// Admin CRUD operations for blog posts.
/// </summary>
[ApiController]
[Authorize(Policy = "RequireAdmin")]
[Route(ApiRoutes.Base + "/admin/blogs")]
[Produces("application/json")]
[Tags("Admin - Blogs")]
public class AdminBlogsController : ControllerBase
{
    private readonly ICreateBlogService _createBlogService;
    private readonly IGetBlogsService _getBlogsService;
    private readonly IUpdateBlogService _updateBlogService;
    private readonly IDeleteBlogService _deleteBlogService;

    public AdminBlogsController(
        ICreateBlogService createBlogService,
        IGetBlogsService getBlogsService,
        IUpdateBlogService updateBlogService,
        IDeleteBlogService deleteBlogService)
    {
        _createBlogService = createBlogService;
        _getBlogsService = getBlogsService;
        _updateBlogService = updateBlogService;
        _deleteBlogService = deleteBlogService;
    }

    /// <summary>List blog posts for CMS management.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBlogs([FromQuery] GetBlogsRequest request)
    {
        request.IncludeDeleted = true;
        var result = await _getBlogsService.ExecuteAsync(request, includeDeleted: true);
        return Ok(result);
    }

    /// <summary>Create a new blog post.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBlog([FromForm] CreateBlogRequest request)
    {
        try
        {
            var blogId = await _createBlogService.ExecuteAsync(request);
            return CreatedAtRoute(string.Empty, new { id = blogId },
                new { message = "Tạo bài viết thành công", blogId });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Update an existing blog post.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBlog(Guid id, [FromForm] UpdateBlogRequest request)
    {
        var isSuccess = await _updateBlogService.ExecuteAsync(id, request);
        if (!isSuccess)
            return NotFound(new { message = "Không tìm thấy bài viết để cập nhật" });

        return Ok(new { message = "Cập nhật bài viết thành công" });
    }

    /// <summary>Delete a blog post.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBlog(Guid id)
    {
        var isSuccess = await _deleteBlogService.ExecuteAsync(id);
        if (!isSuccess)
            return NotFound(new { message = "Không tìm thấy bài viết để xóa" });

        return Ok(new { message = "Xóa bài viết thành công" });
    }
}
