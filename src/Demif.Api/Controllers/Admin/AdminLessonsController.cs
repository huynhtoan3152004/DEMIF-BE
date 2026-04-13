using Demif.Application.Features.Lessons.Admin;
using Demif.Application.Abstractions.Services;
using Demif.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers.Admin;

/// <summary>
/// Admin — Quản lý bài học (Lesson Management).
/// 
/// ╔══════════════════════════════════════════════════════════════════════╗
/// ║  QUY TRÌNH TẠO BÀI (HYBRID WORKFLOW)                              ║
/// ║                                                                    ║
/// ║  Bước 1: Tạo bài nháp (draft)                                     ║
/// ║    → POST /quick-create  (paste SRT/VTT + title)                   ║
/// ║    → POST /from-youtube  (auto-fetch từ YouTube)                   ║
/// ║                                                                    ║
/// ║  Bước 2: Moderator chỉnh sửa lỗ hổng đục lỗ                      ║
/// ║    → PUT  /{id}/dictation-templates  (FE gửi mảng custom)          ║
/// ║    → POST /{id}/regenerate-templates (reset lại auto-generate)     ║
/// ║                                                                    ║
/// ║  Bước 3: Kiểm tra & Xuất bản                                      ║
/// ║    → GET  /{id}/dictation-preview  (preview như User)              ║
/// ║    → PATCH /{id}/status            (draft → published/archived)    ║
/// ╚══════════════════════════════════════════════════════════════════════╝
/// </summary>
[Route("api/admin/lessons")]
[ApiController]
[Authorize(Policy = "RequireModerator")]
public class AdminLessonsController : ControllerBase
{
    private readonly AdminLessonService _adminService;
    private readonly YouTubeLessonService _youTubeService;
    private readonly AdminTranscriptService _transcriptService;
    private readonly IAudioUploadService _audioUploadService;

    public AdminLessonsController(
        AdminLessonService adminService,
        YouTubeLessonService youTubeService,
        AdminTranscriptService transcriptService,
        IAudioUploadService audioUploadService)
    {
        _adminService = adminService;
        _youTubeService = youTubeService;
        _transcriptService = transcriptService;
        _audioUploadService = audioUploadService;
    }

    // ╔══════════════════════════════════════════════════════════════════╗
    // ║  SECTION 1: DANH SÁCH & CHI TIẾT (Read-Only)                   ║
    // ╚══════════════════════════════════════════════════════════════════╝

    /// <summary>
    /// Lấy danh sách tất cả bài học (có phân trang, không lọc premium).
    /// Dùng cho Admin Dashboard hiển thị toàn bộ bài học.
    /// </summary>
    /// <param name="page">Trang hiện tại (mặc định 1)</param>
    /// <param name="pageSize">Số bài mỗi trang (1-100, mặc định 10)</param>
    /// <param name="status">Lọc theo trạng thái: draft, published, archived (tùy chọn)</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] Level? level = null,
        [FromQuery] LessonType? type = null,
        [FromQuery] string? category = null,
        [FromQuery] string? mediaType = null,
        [FromQuery] string? tag = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? isPremiumOnly = null,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await _adminService.GetAllAsync(
            page,
            pageSize,
            status,
            level,
            type,
            category,
            mediaType,
            tag,
            search,
            isPremiumOnly,
            cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Message });

        return Ok(result.Value);
    }

    /// <summary>
    /// Upload riêng file MP3/audio lên Cloudinary và trả về URL.
    /// Endpoint này tách biệt hoàn toàn với YouTube import.
    /// </summary>
    [HttpPost("audio/upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 50_000_000)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAudio([FromForm] UploadLessonAudioRequest request)
    {
        var audioFile = request.AudioFile ?? request.File;

        if (audioFile is null || audioFile.Length == 0)
            return BadRequest(new { error = "AudioFile không được để trống." });

        var isMp3 = audioFile.FileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
            || string.Equals(audioFile.ContentType, "audio/mpeg", StringComparison.OrdinalIgnoreCase)
            || string.Equals(audioFile.ContentType, "audio/mp3", StringComparison.OrdinalIgnoreCase);

        if (!isMp3)
            return BadRequest(new { error = "Chỉ hỗ trợ file MP3/audio hợp lệ." });

        try
        {
            var uploadedUrl = await _audioUploadService.UploadAudioAsync(audioFile, request.FolderName);
            if (string.IsNullOrWhiteSpace(uploadedUrl))
                return BadRequest(new { error = "Upload audio thất bại." });

            return Ok(new UploadLessonAudioResponse
            {
                MediaUrl = uploadedUrl,
                AudioUrl = uploadedUrl,
                MediaType = "audio",
                FolderName = request.FolderName,
                FileName = audioFile.FileName,
                FileSize = audioFile.Length
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết một bài học theo ID (bao gồm cả Transcript, Templates...).
    /// </summary>
    /// <param name="id">Lesson ID (GUID)</param>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    // ╔══════════════════════════════════════════════════════════════════╗
    // ║  SECTION 2: TẠO BÀI HỌC (Bước 1 — Khởi tạo Draft)            ║
    // ║  Có 2 cách tạo: quick-create hoặc from-youtube                 ║
    // ╚══════════════════════════════════════════════════════════════════╝

    /// <summary>
    /// Tạo nhanh bài học — Admin/Mod paste SRT/VTT/plain transcript + thông tin cơ bản.
    /// Backend auto-generate: TimedTranscript, FullTranscript, DictationTemplates (bản nháp).
    /// Sau khi tạo, Moderator dùng PUT /{id}/dictation-templates để chỉnh sửa lỗ hổng.
    /// 
    /// Workflow: quick-create → chỉnh dictation-templates → preview → publish
    /// </summary>
    /// <remarks>
    /// Format hỗ trợ: "srt", "vtt", "plain".
    /// Mặc định status = "draft". PATCH /status để publish khi sẵn sàng.
    /// </remarks>
    [HttpPost("quick-create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> QuickCreate(
        [FromBody] QuickCreateLessonRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.QuickCreateAsync(request, cancellationToken);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Message });

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Value.LessonId },
            result.Value);
    }

    /// <summary>
    /// Tạo bài học từ YouTube URL — Auto-fetch metadata + captions.
    /// Backend tự lấy: tiêu đề, thumbnail, phụ đề, thời lượng.
    /// Sau khi tạo, DictationTemplates được auto-generate cho 4 level.
    /// 
    /// ⚠️ Nếu video không có CC (phụ đề), Moderator cần dùng 
    ///    PATCH /{id}/transcript để upload transcript thủ công sau.
    /// </summary>
    [HttpPost("from-youtube")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

    // ╔══════════════════════════════════════════════════════════════════╗
    // ║  SECTION 3: CHỈNH SỬA TEMPLATES & TRANSCRIPT (Bước 2)          ║
    // ║  Moderator điều chỉnh lỗ hổng đục lỗ và nội dung transcript    ║
    // ╚══════════════════════════════════════════════════════════════════╝

    /// <summary>
    /// Ghi đè mảng DictationTemplates bằng dữ liệu custom từ Frontend.
    /// 
    /// FE gửi toàn bộ mảng JSON chứa các lỗ hổng (isBlank=true) mà Mod đã chọn.
    /// Backend lưu đè trực tiếp, KHÔNG chạy thuật toán đục lỗ tự động.
    /// 
    /// Quy trình Hybrid: Backend auto-gen bản nháp → Mod sửa lại → PUT endpoint này.
    /// </summary>
    /// <param name="id">Lesson ID</param>
    /// <param name="request">JSON chứa mảng DictationTemplates đã custom</param>
    [HttpPut("{id:guid}/dictation-templates")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateDictationTemplates(
        Guid id,
        [FromBody] UpdateDictationTemplatesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateDictationTemplatesAsync(id, request, cancellationToken);
        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound" => NotFound(new { error = result.Error.Message }),
                _ => BadRequest(new { error = result.Error.Message })
            };
        }
        return Ok(new { message = "Custom DictationTemplates updated successfully." });
    }

    /// <summary>
    /// Re-generate DictationTemplates tự động (reset lại bản nháp được máy đục lỗ).
    /// Dùng khi Moderator muốn bắt đầu lại từ đầu thay vì sửa bản cũ.
    /// Sau khi gọi, dùng GET /{id}/dictation-preview để xem kết quả mới.
    /// </summary>
    [HttpPost("{id:guid}/regenerate-templates")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    /// Upload / cập nhật transcript thủ công — hỗ trợ VTT, SRT, hoặc plain text.
    /// Dùng khi video không có caption hoặc caption YouTube bị sai chính tả.
    /// 
    /// ⚠️ Khi cập nhật transcript, DictationTemplates cũ sẽ bị RESET.
    ///    Cần gọi lại PUT /{id}/dictation-templates hoặc POST /{id}/regenerate-templates.
    /// </summary>
    [HttpPatch("{id:guid}/transcript")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

    // ╔══════════════════════════════════════════════════════════════════╗
    // ║  SECTION 4: KIỂM TRA & XUẤT BẢN (Bước 3 — Preview & Publish)  ║
    // ╚══════════════════════════════════════════════════════════════════╝

    /// <summary>
    /// Xem trước bài dictation với góc nhìn User (hiển thị đáp án).
    /// Dùng để kiểm tra các lỗ hổng đục lỗ trước khi xuất bản.
    /// Admin thấy toàn bộ segments + đáp án để đối chiếu với transcript.
    /// </summary>
    [HttpGet("{id:guid}/dictation-preview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
    /// Thay đổi trạng thái bài học: draft → published → archived.
    /// 
    /// Guard rules:
    ///   - Không cho publish nếu chưa có TimedTranscript.
    ///   - Không cho publish nếu chưa có DictationTemplates.
    /// </summary>
    /// <param name="id">Lesson ID</param>
    /// <param name="request">Body chứa trường "status": "published" | "draft" | "archived"</param>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

    // ╔══════════════════════════════════════════════════════════════════╗
    // ║  SECTION 5: QUẢN LÝ CHUNG (Delete, Update metadata)            ║
    // ╚══════════════════════════════════════════════════════════════════╝

    /// <summary>
    /// Cập nhật metadata bài học (Tiêu đề, Level, Premium, Category...).
    /// Nếu FullTranscript thay đổi, DictationTemplates sẽ được re-generate tự động.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateLessonMetadataRequest request,
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
    /// Xóa bài học (soft delete — chuyển status sang "archived").
    /// Bài học sẽ không hiển thị cho User nhưng vẫn còn trong Database.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

    // ╔══════════════════════════════════════════════════════════════════╗
    // ║  SECTION 6: YOUTUBE PREVIEW (Hỗ trợ — không ghi DB)            ║
    // ╚══════════════════════════════════════════════════════════════════╝

    /// <summary>
    /// Preview metadata YouTube video trước khi tạo bài.
    /// Kiểm tra xem video có phụ đề (CC) hay không.
    /// KHÔNG ghi vào Database — chỉ trả về dữ liệu để FE hiển thị.
    /// </summary>
    [HttpGet("youtube/preview")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    /// Dùng để Admin preview transcript trước khi chọn import.
    /// </summary>
    [HttpGet("youtube/transcripts")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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


}
