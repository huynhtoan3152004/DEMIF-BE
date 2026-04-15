using Demif.Application.Abstractions.Services;
using Demif.Application.Features.Lessons.GetLessonById;
using Demif.Application.Features.Lessons.GetLessons;
using Demif.Application.Features.Lessons.GetLessonSegments;
using Demif.Application.Features.Lessons.Tracking;
using Demif.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers;

/// <summary>
/// API Controller cho Lessons (public + premium)
/// Chứa các endpoint liên quan đến chi tiết, danh sách lesson và các segments
/// </summary>
[Route("api/lessons")]
[ApiController]
public class LessonsController : ControllerBase
{
    private readonly GetLessonsService _getLessonsService;
    private readonly GetLessonByIdService _getLessonByIdService;
    private readonly GetLessonSegmentsService _getSegmentsService;
    private readonly SyncProgressService _syncProgressService;
    private readonly GetCompletedSegmentsService _completedSegmentsService;
        private readonly RecordLessonAccessService _recordLessonAccessService;
    private readonly ICurrentUserService _currentUserService;

    public LessonsController(
        GetLessonsService getLessonsService,
        GetLessonByIdService getLessonByIdService,
        GetLessonSegmentsService getSegmentsService,
        SyncProgressService syncProgressService,
        GetCompletedSegmentsService completedSegmentsService,
        RecordLessonAccessService recordLessonAccessService,
        ICurrentUserService currentUserService)
    {
        _getLessonsService = getLessonsService;
        _getLessonByIdService = getLessonByIdService;
        _getSegmentsService = getSegmentsService;
        _syncProgressService = syncProgressService;
        _completedSegmentsService = completedSegmentsService;
        _recordLessonAccessService = recordLessonAccessService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Lấy danh sách lessons với pagination.
    /// Hỗ trợ filter: level, type, category, mediaType, tag, search.
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

        if (userId.HasValue)
        {
            await _recordLessonAccessService.RecordAsync(userId.Value, id, "detail", cancellationToken);
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

        if (userId.HasValue)
        {
            await _recordLessonAccessService.RecordAsync(userId.Value, id, "segments", cancellationToken);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Đồng bộ hoá tiến trình của user trong bài học (segment nào đang học / đã hoàn thành).
    /// </summary>
    [HttpPost("{id:guid}/sync-progress")]
    public async Task<IActionResult> SyncProgress(
        Guid id,
        [FromBody] SyncProgressRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdOrNull();
        if (userId is null) return Unauthorized();

        var result = await _syncProgressService.ExecuteAsync(userId.Value, id, request, cancellationToken);
        
        if (result.IsFailure)
        {
            return BadRequest(new { error = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Lấy tiến độ user trong 1 bài học (danh sách segments đã hoàn thành + overall status).
    /// GET /api/lessons/{id}/my-progress
    /// </summary>
    [HttpGet("{id:guid}/my-progress")]
    [Authorize]
    [ProducesResponseType(typeof(LessonProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyProgress(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdOrNull();
        if (userId is null) return Unauthorized();

        var result = await _completedSegmentsService.ExecuteAsync(userId.Value, id, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound" => NotFound(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        return Ok(result.Value);
    }

    private Guid? GetUserIdOrNull()
    {
        return _currentUserService.UserId;
    }
}
