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
    private readonly AdminTranscriptService _transcriptService;

    public AdminLessonsController(
        AdminLessonService adminService,
        YouTubeLessonService youTubeService,
        AdminTranscriptService transcriptService)
    {
        _adminService = adminService;
        _youTubeService = youTubeService;
        _transcriptService = transcriptService;
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
    /// Lấy transcript YouTube theo nhiều ngôn ngữ (không ghi DB).
    /// Dùng để FE/Admin preview transcript trước khi import.
    /// </summary>
    [HttpGet("youtube/transcripts")]
    public async Task<IActionResult> GetYouTubeTranscripts(
        [FromQuery] string url,
        [FromQuery] string preferredLanguage = "en",
        [FromQuery] bool includeText = true,
        CancellationToken cancellationToken = default)
    {
        var result = await _youTubeService.GetTranscriptsAsync(
            url,
            preferredLanguage,
            includeText,
            cancellationToken);

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

    /// <summary>
    /// Admin xem toàn bộ segments + đáp án (chưa xóa answers).
    /// Kiểm tra tất cả segment có đúng với transcript chưa trước khi publish.
    /// GET /api/admin/lessons/{id}/dictation-preview
    /// </summary>
    [HttpGet("{id:guid}/dictation-preview")]
    public async Task<IActionResult> GetDictationPreview(Guid id, CancellationToken cancellationToken)
    {
        var result = await _transcriptService.GetDictationPreviewAsync(id, cancellationToken);

        if (result.IsFailure)
            return result.Error.Code switch
            {
                "NotFound" => NotFound(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };

        return Ok(result.Value);
    }

    /// <summary>
    /// Upload / cập nhật transcript thủ công — hỗ trợ VTT, SRT, hoặc plain text.
    /// Dùng khi video không có caption hoặc caption YouTube bị sai chính tả.
    /// PATCH /api/admin/lessons/{id}/transcript
    /// </summary>
    [HttpPatch("{id:guid}/transcript")]
    public async Task<IActionResult> UpdateTranscript(
        Guid id,
        [FromBody] UpdateTranscriptRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _transcriptService.UpdateTranscriptAsync(id, request, cancellationToken);

        if (result.IsFailure)
            return result.Error.Code switch
            {
                "NotFound"   => NotFound(new { error = result.Error.Message }),
                "Validation" => BadRequest(new { error = result.Error.Message }),
                _            => BadRequest(new { error = result.Error.Message })
            };

        return Ok(result.Value);
    }

    /// <summary>
    /// Thay đổi trạng thái: draft → published → archived.
    /// Guard: không publish được nếu chưa có TimedTranscript.
    /// PATCH /api/admin/lessons/{id}/status
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateLessonStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _transcriptService.UpdateStatusAsync(id, request, cancellationToken);

        if (result.IsFailure)
            return result.Error.Code switch
            {
                "NotFound"   => NotFound(new { error = result.Error.Message }),
                "Validation" => BadRequest(new { error = result.Error.Message }),
                _            => BadRequest(new { error = result.Error.Message })
            };

        return Ok(result.Value);
    }
}
