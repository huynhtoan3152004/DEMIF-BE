using Demif.Application.Features.Lessons.CheckSegment;
using Demif.Application.Features.Lessons.GetDictationExercise;
using Demif.Application.Features.Lessons.GetLessonById;
using Demif.Application.Features.Lessons.GetLessons;
using Demif.Application.Features.Lessons.GetLessonSegments;
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
    private readonly GetLessonSegmentsService _getSegmentsService;
    private readonly CheckSegmentService _checkSegmentService;

    public LessonsController(
        GetLessonsService getLessonsService,
        GetLessonByIdService getLessonByIdService,
        GetDictationExerciseService getDictationExerciseService,
        SubmitDictationService submitDictationService,
        GetLessonSegmentsService getSegmentsService,
        CheckSegmentService checkSegmentService)
    {
        _getLessonsService = getLessonsService;
        _getLessonByIdService = getLessonByIdService;
        _getDictationExerciseService = getDictationExerciseService;
        _submitDictationService = submitDictationService;
        _getSegmentsService = getSegmentsService;
        _checkSegmentService = checkSegmentService;
    }

    /// <summary>
    /// Lấy danh sách lessons với pagination.
    /// Hỗ trợ filter: level (string "Beginner" hoặc số 0), type, category.
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
    /// Lấy chi tiết lesson (kiểm tra premium access).
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
    /// GET /api/lessons/{id}/dictation?level=Beginner (hoặc ?level=0)
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
    /// Submit dictation answers → chấm điểm + lưu kết quả.
    /// POST /api/lessons/{id}/dictation/submit
    /// Level trong body: "Beginner"/"Intermediate"/"Advanced"/"Expert"
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

    /// <summary>
    /// Lấy danh sách segments của bài theo level config.
    /// GET /api/lessons/{id}/segments?level=Intermediate
    /// 
    /// LevelConfig trong response cho FE biết:
    /// - showTranscriptBefore: có hiện text trước khi user gõ không
    /// - showTranscriptAfter: có auto-hiện transcript sau check không
    /// - maxReplays: số lần replay mỗi segment (-1 = unlimited)
    /// </summary>
    [HttpGet("{id:guid}/segments")]
    public async Task<IActionResult> GetLessonSegments(
        Guid id,
        [FromQuery] string level = "Intermediate",
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdOrNull();
        var result = await _getSegmentsService.ExecuteAsync(id, level, userId, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound"   => NotFound(new { error = result.Error.Message }),
                "Forbidden"  => StatusCode(403, new { error = result.Error.Message }),
                "Validation" => BadRequest(new { error = result.Error.Message }),
                _            => BadRequest(new { error = result.Error.Message })
            };
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Check một segment: user gõ tự do → so sánh word-by-word với transcript gốc.
    /// POST /api/lessons/{id}/segments/{segmentIndex}/check
    /// Luôn trả transcript trong response (học từ lỗi — Option A + C).
    /// </summary>
    [HttpPost("{id:guid}/segments/{segmentIndex:int}/check")]
    [Authorize]
    public async Task<IActionResult> CheckSegment(
        Guid id,
        int segmentIndex,
        [FromBody] CheckSegmentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrNull()!.Value;
        var result = await _checkSegmentService.ExecuteAsync(id, segmentIndex, request, userId, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound"   => NotFound(new { error = result.Error.Message }),
                "Validation" => BadRequest(new { error = result.Error.Message }),
                _            => BadRequest(new { error = result.Error.Message })
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

