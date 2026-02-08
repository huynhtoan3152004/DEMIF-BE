using Demif.Application.Features.Lessons.GetLessonById;
using Demif.Application.Features.Lessons.GetLessons;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers;

/// <summary>
/// API Controller cho Lessons (public + premium)
/// </summary>
[Route("api/lessons")]
[ApiController]
public class LessonsController : ControllerBase
{
    private readonly GetLessonsService _getLessonsService;
    private readonly GetLessonByIdService _getLessonByIdService;

    public LessonsController(
        GetLessonsService getLessonsService,
        GetLessonByIdService getLessonByIdService)
    {
        _getLessonsService = getLessonsService;
        _getLessonByIdService = getLessonByIdService;
    }

    /// <summary>
    /// Lấy danh sách lessons với pagination (lọc premium theo subscription)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLessons(
        [FromQuery] GetLessonsRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrNull();
        var result = await _getLessonsService.ExecuteAsync(request, userId, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Lấy chi tiết lesson (kiểm tra premium access)
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetLessonById(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrNull();
        var result = await _getLessonByIdService.ExecuteAsync(id, userId, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound" => NotFound(new { error = result.Error.Message }),
                "Forbidden" => StatusCode(403, new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        return Ok(result.Value);
    }

    private Guid? GetUserIdOrNull()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
