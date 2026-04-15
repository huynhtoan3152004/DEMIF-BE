using Demif.Application.Features.Lessons.Admin;
using Demif.Application.Abstractions.Services;
using Demif.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Demif.Api.Controllers.Admin;

/// <summary>
/// Admin — Quản lý bài học (Lesson Management).
/// </summary>
//
// QUY TRÌNH TẠO BÀI (HYBRID WORKFLOW)
// Bước 1: Tạo bài nháp (draft)
//   → POST /quick-create  (paste SRT/VTT + title)
//   → POST /from-youtube  (auto-fetch từ YouTube)
// Bước 2: Moderator chỉnh sửa lỗ hổng đục lỗ
//   → PUT  /{id}/dictation-templates  (FE gửi mảng custom)
//   → POST /{id}/regenerate-templates (reset lại auto-generate)
// Bước 3: Kiểm tra & Xuất bản
//   → GET  /{id}/dictation-preview  (preview như User)
//   → PATCH /{id}/status            (draft → published/archived)
[Route("api/admin/lessons")]
[ApiController]
[Authorize(Policy = "RequireModerator")]
public class AdminLessonsController : ControllerBase
{
    private readonly AdminLessonService _adminService;
    private readonly YouTubeLessonService _youTubeService;
    private readonly AdminTranscriptService _transcriptService;
    private readonly IAudioUploadService _audioUploadService;
    private readonly ILogger<AdminLessonsController> _logger;

    public AdminLessonsController(
        AdminLessonService adminService,
        YouTubeLessonService youTubeService,
        AdminTranscriptService transcriptService,
        IAudioUploadService audioUploadService,
        ILogger<AdminLessonsController> logger)
    {
        _adminService = adminService;
        _youTubeService = youTubeService;
        _transcriptService = transcriptService;
        _audioUploadService = audioUploadService;
        _logger = logger;
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
    /// <param name="level">Lọc theo level: Beginner, Intermediate, Advanced, Expert</param>
    /// <param name="type">Lọc theo lesson type: Dictation, Shadowing</param>
    /// <param name="category">Lọc theo category</param>
    /// <param name="mediaType">Lọc theo media type: audio, youtube, video</param>
    /// <param name="tag">Lọc theo tag</param>
    /// <param name="search">Tìm theo tiêu đề hoặc mô tả</param>
    /// <param name="isPremiumOnly">Lọc bài premium</param>
    /// <param name="cancellationToken">Token hủy request</param>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? level = null,
        [FromQuery] string? type = null,
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
    /// Accept MỌI field name từ FormData (AudioFile, File, audio, file, mp3File...).
    /// </summary>
    [HttpPost("audio/upload")]
    [RequestSizeLimit(50_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 50_000_000)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAudio()
    {
        _logger.LogInformation(
            "[AudioUpload] Request received. ContentType: {ContentType}, ContentLength: {Length}, HasFormContentType: {HasForm}",
            Request.ContentType, Request.ContentLength, Request.HasFormContentType);

        if (!Request.HasFormContentType)
        {
            _logger.LogWarning("[AudioUpload] Request is NOT multipart/form-data. ContentType: {ContentType}", Request.ContentType);
            return BadRequest(new { error = "Request phải là multipart/form-data." });
        }

        var form = await Request.ReadFormAsync();

        _logger.LogInformation(
            "[AudioUpload] Form files count: {Count}, Field names: [{Fields}]",
            form.Files.Count,
            string.Join(", ", form.Files.Select(f => $"{f.Name}={f.FileName}({f.Length}b)")));

        // Accept file từ BẤT KỲ field name nào
        var audioFile = form.Files.GetFile("AudioFile")
                     ?? form.Files.GetFile("audioFile")
                     ?? form.Files.GetFile("File")
                     ?? form.Files.GetFile("file")
                     ?? form.Files.GetFile("audio")
                     ?? form.Files.GetFile("mp3File")
                     ?? (form.Files.Count > 0 ? form.Files[0] : null);

        if (audioFile is null || audioFile.Length == 0)
        {
            _logger.LogWarning("[AudioUpload] No audio file found in form. Files count: {Count}", form.Files.Count);
            return BadRequest(new { error = "AudioFile không được để trống. Gửi file trong FormData với field name bất kỳ." });
        }

        _logger.LogInformation(
            "[AudioUpload] File found: Name={Name}, FileName={FileName}, ContentType={ContentType}, Size={Size}b",
            audioFile.Name, audioFile.FileName, audioFile.ContentType, audioFile.Length);

        // Accept wider audio MIME types
        var allowedExtensions = new[] { ".mp3", ".wav", ".ogg", ".m4a", ".aac", ".wma", ".flac" };
        var allowedMimeTypes = new[] { "audio/mpeg", "audio/mp3", "audio/wav", "audio/ogg", "audio/x-m4a", "audio/aac", "audio/x-wav", "audio/webm" };

        var hasValidExtension = allowedExtensions.Any(ext => audioFile.FileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        var hasValidMime = allowedMimeTypes.Any(mime => string.Equals(audioFile.ContentType, mime, StringComparison.OrdinalIgnoreCase));

        if (!hasValidExtension && !hasValidMime)
        {
            _logger.LogWarning("[AudioUpload] Invalid file type: {FileName}, ContentType={ContentType}", audioFile.FileName, audioFile.ContentType);
            return BadRequest(new { error = $"File type không hợp lệ. Hỗ trợ: {string.Join(", ", allowedExtensions)}. ContentType nhận được: {audioFile.ContentType}" });
        }

        var folderName = form.TryGetValue("FolderName", out var folderVal) ? folderVal.ToString() : "demif-lessons/audio";

        try
        {
            var uploadedUrl = await _audioUploadService.UploadAudioAsync(audioFile, folderName);
            if (string.IsNullOrWhiteSpace(uploadedUrl))
            {
                _logger.LogError("[AudioUpload] Cloudinary returned null/empty URL");
                return BadRequest(new { error = "Upload audio thất bại — Cloudinary không trả URL." });
            }

            _logger.LogInformation("[AudioUpload] Success! URL: {Url}", uploadedUrl);

            return Ok(new UploadLessonAudioResponse
            {
                MediaUrl = uploadedUrl,
                AudioUrl = uploadedUrl,
                MediaType = "audio",
                FolderName = folderName,
                FileName = audioFile.FileName,
                FileSize = audioFile.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AudioUpload] Exception during upload: {Message}", ex.Message);
            return StatusCode(500, new { error = $"Lỗi upload: {ex.Message}" });
        }
    }

    /// <summary>
    /// Lấy chi tiết một bài học theo ID (bao gồm cả Transcript, Templates...).
    /// </summary>
    /// <param name="id">Lesson ID (GUID)</param>
    /// <param name="cancellationToken">Token hủy request</param>
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
    /// <param name="cancellationToken">Token hủy request</param>
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
    /// <param name="cancellationToken">Token hủy request</param>
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
