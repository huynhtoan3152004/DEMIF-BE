using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Demif.Application.Features.Blog;
using Demif.Api.Contracts;
using Demif.Domain.Entities;
using Demif.Application.Common.Models;

namespace Demif.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlogController : ControllerBase
{
    private readonly BlogService _blogService;

    public BlogController(BlogService blogService)
    {
        _blogService = blogService;
    }


    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<Post>>), 200)]
    public async Task<IActionResult> GetList()
    {
        var posts = await _blogService.GetListAsync();
        return Ok(ApiResponse<IEnumerable<Post>>.Ok(posts));
    }


    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePostRequest request)
    {
        var result = await _blogService.CreateAsync(request.Title, request.Content);

        if (result.IsFailure)
            return BadRequest(ApiResponse<string>.Fail(result.Error.Message));

        return Ok(ApiResponse<string>.Ok("Create post successfull."));
    }


    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePostRequest request)
    {
        var result = await _blogService.UpdateAsync(id, request.Title, request.Content);

        if (result.IsFailure)
        {
            if (result.Error.Code == "Auth.Forbidden") return Forbid();
            return BadRequest(ApiResponse<string>.Fail(result.Error.Message));
        }

        return Ok(ApiResponse<string>.Ok("Edit successfull."));
    }


    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _blogService.DeleteAsync(id);

        if (result.IsFailure)
            return BadRequest(ApiResponse<string>.Fail(result.Error.Message));

        return Ok(ApiResponse<string>.Ok("Deleted post."));
    }


    [Authorize]
    [HttpPost("{id}/like")]
    public async Task<IActionResult> ToggleLike(Guid id)
    {
        var result = await _blogService.ToggleLikeAsync(id);

        if (result.IsFailure)
            return BadRequest(ApiResponse<string>.Fail(result.Error.Message));

        return Ok(ApiResponse<bool>.Ok(result.Value, result.Value ? "Liked" : "Unliked"));
    }


    [Authorize]
    [HttpPost("{id}/comment")]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return BadRequest(ApiResponse<string>.Fail("Comment is not null."));

        var result = await _blogService.AddCommentAsync(id, content);

        if (result.IsFailure)
            return BadRequest(ApiResponse<string>.Fail(result.Error.Message));

        return Ok(ApiResponse<string>.Ok("Commented."));
    }
}

public record CreatePostRequest(string Title, string Content);
public record UpdatePostRequest(string Title, string Content);