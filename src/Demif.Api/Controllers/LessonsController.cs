using Demif.Application.Features.Lessons.GetDictationExercise;
using Demif.Application.Features.Lessons.GetLessonById;
using Demif.Application.Features.Lessons.GetLessons;
using Demif.Application.Features.Lessons.SubmitDictation;
using Demif.Domain.Enums;
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

    /// <summary>
    /// Lấy danh sách lessons với pagination
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

    /// <summary>
    /// Lấy dictation exercise (blanks KHÔNG có đáp án).
    /// GET /api/lessons/{id}/dictation?level=Beginner
    /// </summary>
    [HttpGet("{id:guid}/dictation")]
    public async Task<IActionResult> GetDictationExercise(
        Guid id,
        [FromQuery] Level level = Level.Beginner,
        CancellationToken cancellationToken = default)
    {
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
    /// Submit dictation answers → chấm điểm + lưu kết quả.
    /// POST /api/lessons/{id}/dictation/submit
    /// User điền hết blanks → submit 1 lần → nhận kết quả + đáp án đúng.
    /// </summary>
    [HttpPost("{id:guid}/dictation/submit")]
    [Authorize]
    public async Task<IActionResult> SubmitDictation(
        Guid id,
        [FromBody] DictationSubmitRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrNull();
        if (!userId.HasValue)
        {
            return Unauthorized(new { error = "Vui lòng đăng nhập để nộp bài." });
        }

        var result = await _submitDictationService.ExecuteAsync(id, userId.Value, request, cancellationToken);

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
