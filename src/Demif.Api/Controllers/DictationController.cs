using Demif.Application.Features.Lessons.CheckSegment;
using Demif.Application.Features.Lessons.GetDictationExercise;
using Demif.Application.Features.Lessons.SubmitDictation;
using Demif.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Demif.Api.Controllers;

/// <summary>
/// API Controller dành riêng cho các tác vụ Dictation (Điền từ vào chỗ trống).
/// </summary>
[Route("api/lessons")]
[ApiController]
public class DictationController : ControllerBase
{
    private readonly GetDictationExerciseService _getDictationExerciseService;
    private readonly SubmitDictationService _submitDictationService;
    private readonly CheckSegmentService _checkSegmentService;

    public DictationController(
        GetDictationExerciseService getDictationExerciseService,
        SubmitDictationService submitDictationService,
        CheckSegmentService checkSegmentService)
    {
        _getDictationExerciseService = getDictationExerciseService;
        _submitDictationService = submitDictationService;
        _checkSegmentService = checkSegmentService;
    }

    /// <summary>
    /// Lấy dictation exercise (blanks KHÔNG có đáp án).
    /// GET /api/lessons/{id}/dictation?level=Beginner
    /// </summary>
    [HttpGet("{id:guid}/dictation")]
    public async Task<IActionResult> GetDictationExercise(
        Guid id,
        [FromQuery] string levelStr = "Beginner",
        CancellationToken cancellationToken = default)
    {
        Level level;
        if (int.TryParse(levelStr, out var levelInt) && Enum.IsDefined(typeof(Level), levelInt))
            level = (Level)levelInt;
        else if (!Enum.TryParse<Level>(levelStr, ignoreCase: true, out level))
            return BadRequest(new { error = $"Level '{levelStr}' không hợp lệ. Dùng: Beginner, Intermediate, Advanced, Expert." });

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
    /// </summary>
    [HttpPost("{id:guid}/dictation/submit")]
    [Authorize]
    public async Task<IActionResult> SubmitDictation(
        Guid id,
        [FromBody] DictationSubmitRequest request,
        CancellationToken cancellationToken)
    {
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
    /// Check một segment: user gõ tự do → so sánh word-by-word với transcript.
    /// POST /api/lessons/{id}/segments/{segmentIndex}/check
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

    /// <summary>
    /// Check một segment từ kết quả speech recognition (STT) của browser (Text Fallback).
    /// Đây là một feature hỗ trợ Dictation bằng Voice.
    /// POST /api/lessons/{id}/segments/{segmentIndex}/check-voice
    /// </summary>
    [HttpPost("{id:guid}/segments/{segmentIndex:int}/check-voice")]
    [Authorize]
    public async Task<IActionResult> CheckVoiceSegment(
        Guid id,
        int segmentIndex,
        [FromBody] CheckVoiceSegmentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrNull()!.Value;
        var result = await _checkSegmentService.ExecuteVoiceAsync(id, segmentIndex, request, userId, cancellationToken);

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
        var userIdClaim = User.FindFirst("userId")?.Value
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
