using Demif.Application.Features.Lessons.GetLessons;
using Demif.Application.Features.Lessons.GetLessonById;
using Demif.Application.Features.Lessons.GetDictationExercise;
using Demif.Application.Features.Lessons.SubmitDictation;
using Demif.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers;

/// <summary>
/// Lessons — Browse lessons, practice dictation exercises
/// </summary>
[Route("api/lessons")]
[ApiController]
public class LessonsController : ControllerBase
{
    private readonly GetLessonsService _getLessonsService;
    private readonly GetLessonByIdService _getLessonByIdService;
    private readonly GetDictationExerciseService _getDictationExerciseService;
    private readonly SubmitDictationService _submitDictationService;

    public LessonsController(
        GetLessonsService getLessonsService,
        GetLessonByIdService getLessonByIdService,
        GetDictationExerciseService getDictationExerciseService,
        SubmitDictationService submitDictationService)
    {
        _getLessonsService = getLessonsService;
        _getLessonByIdService = getLessonByIdService;
        _getDictationExerciseService = getDictationExerciseService;
        _submitDictationService = submitDictationService;
    }

    // ═══════════════════════════════════════════════════════════════
    // Lesson Listing & Details
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Get paginated list of lessons. Filter by: level, type, category.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLessons(
        [FromQuery] GetLessonsRequest request,
        CancellationToken cancellationToken)
    {
        // Clamp pagination để tránh query quá lớn
        request.Page = Math.Max(1, request.Page);
        request.PageSize = Math.Clamp(request.PageSize, 1, 100);

        var userId = GetUserIdOrNull();
        var result = await _getLessonsService.ExecuteAsync(request, userId, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get lesson detail (checks premium access).
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

    // ═══════════════════════════════════════════════════════════════
    // Dictation Exercises
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Get dictation exercise (blanks without answers).
    /// Level: Beginner|Intermediate|Advanced|Expert (or 0|1|2|3).
    /// </summary>
    [HttpGet("{id:guid}/dictation")]
    public async Task<IActionResult> GetDictationExercise(
        Guid id,
        [FromQuery] string levelStr = "Beginner",
        CancellationToken cancellationToken = default)
    {
        // Accept both "Beginner"/0/1/2/3 — parse linh hoạt
        Level level;
        if (int.TryParse(levelStr, out var levelInt) && Enum.IsDefined(typeof(Level), levelInt))
        {
            level = (Level)levelInt;
        }
        else if (!Enum.TryParse<Level>(levelStr, ignoreCase: true, out level))
        {
            return BadRequest(new { error = $"Level '{levelStr}' không hợp lệ. Dùng: Beginner, Intermediate, Advanced, Expert (hoặc 0,1,2,3)." });
        }

        var userId = GetUserIdOrNull();
        var result = await _getDictationExerciseService.ExecuteAsync(id, level, userId, cancellationToken);

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

    /// <summary>
    /// Submit dictation answers — scoring + save results.
    /// Requires authentication.
    /// </summary>
    [HttpPost("{id:guid}/dictation/submit")]
    [Authorize]
    public async Task<IActionResult> SubmitDictation(
        Guid id,
        [FromBody] DictationSubmitRequest request,
        CancellationToken cancellationToken)
    {
        // userId guaranteed by [Authorize]
        var userId = GetUserIdOrNull()!.Value;

        var result = await _submitDictationService.ExecuteAsync(id, userId, request, cancellationToken);

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

