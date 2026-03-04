using Demif.Application.Features.Lessons.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers.Admin;

/// <summary>
/// Admin — Lesson Management (CRUD, templates, YouTube import)
/// </summary>
[Route("api/admin/lessons")]
[ApiController]
[Authorize(Policy = "RequireStaff")]
public class AdminLessonsController : ControllerBase
{
    private readonly AdminLessonService _adminService;
    private readonly YouTubeLessonService _youTubeService;

    public AdminLessonsController(
        AdminLessonService adminService,
        YouTubeLessonService youTubeService)
    {
        _adminService = adminService;
        _youTubeService = youTubeService;
    }

    // ═══════════════════════════════════════════════════════════════
    // Lesson CRUD
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// List all lessons with pagination (no premium filter).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        // Clamp để tránh query nguy hiểm
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await _adminService.GetAllAsync(page, pageSize, status, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Get lesson detail.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _adminService.GetByIdAsync(id, cancellationToken);

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

    /// <summary>
    /// Create a new lesson (auto-generates DictationTemplates from FullTranscript).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUpdateLessonRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.CreateAsync(request, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Message });

        return CreatedAtAction(nameof(GetById), new { id = result.Value }, new { id = result.Value });
    }

    /// <summary>
    /// Update lesson (re-generates templates if transcript changed).
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] CreateUpdateLessonRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateAsync(id, request, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound" => NotFound(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Delete lesson (soft delete — archived).
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _adminService.DeleteAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound" => NotFound(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        return NoContent();
    }

    /// <summary>
    /// Re-generate DictationTemplates for an existing lesson.
    /// Useful for refreshing templates without changing lesson data.
    /// </summary>
    [HttpPost("{id:guid}/regenerate-templates")]
    public async Task<IActionResult> RegenerateTemplates(Guid id, CancellationToken cancellationToken)
    {
        var result = await _adminService.RegenerateTemplatesAsync(id, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound" => NotFound(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }

        return Ok(new { message = "DictationTemplates regenerated successfully." });
    }

    // ═══════════════════════════════════════════════════════════════
    // YouTube Integration Endpoints
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Preview YouTube video before creating a lesson.
    /// Check metadata and available captions.
    /// </summary>
    [HttpGet("youtube/preview")]
    public async Task<IActionResult> YouTubePreview(
        [FromQuery] string url,
        CancellationToken cancellationToken)
    {
        var result = await _youTubeService.PreviewAsync(url, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Create lesson from YouTube URL.
    /// Auto-fetches metadata + captions, generates DictationTemplates for 4 levels.
    /// </summary>
    [HttpPost("from-youtube")]
    public async Task<IActionResult> CreateFromYouTube(
        [FromBody] CreateLessonFromYouTubeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _youTubeService.CreateFromYouTubeAsync(request, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Message });

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value.LessonId },
            result.Value);
    }
}
