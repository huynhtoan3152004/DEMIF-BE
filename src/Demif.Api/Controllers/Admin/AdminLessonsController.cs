using Demif.Application.Features.Lessons.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers.Admin;

/// <summary>
/// Admin API Controller cho quản lý Lessons
/// </summary>
[Route("api/admin/lessons")]
[ApiController]
[Authorize(Policy = "RequireStaff")]
public class AdminLessonsController : ControllerBase
{
    private readonly AdminLessonService _adminService;
    private readonly UploadLessonAudioService _uploadService;

    public AdminLessonsController(
        AdminLessonService adminService,
        UploadLessonAudioService uploadService)
    {
        _adminService = adminService;
        _uploadService = uploadService;
    }

    /// <summary>
    /// Lấy tất cả lessons với pagination (không filter premium)
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
    /// Lấy chi tiết lesson
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
    /// Tạo lesson mới (auto-generate DictationTemplates từ FullTranscript)
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
    /// Cập nhật lesson (re-generate templates nếu transcript thay đổi)
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
    /// Xóa lesson (soft delete - archived)
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
    /// Re-generate DictationTemplates cho lesson hiện có
    /// Hữu ích khi muốn refresh templates mà không thay đổi lesson data
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

    /// <summary>
    /// Upload file MP3 lên Firebase Storage và gắn URL vào lesson.
    /// POST /api/admin/lessons/{id}/upload-audio
    /// Content-Type: multipart/form-data
    /// Body: file (IFormFile)
    /// </summary>
    [HttpPost("{id:guid}/upload-audio")]
    [RequestSizeLimit(52_428_800)] // 50MB
    public async Task<IActionResult> UploadAudio(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Vui lòng chọn file âm thanh." });

        await using var stream = file.OpenReadStream();

        var result = await _uploadService.ExecuteAsync(
            id,
            stream,
            file.FileName,
            file.ContentType,
            cancellationToken);

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
}
