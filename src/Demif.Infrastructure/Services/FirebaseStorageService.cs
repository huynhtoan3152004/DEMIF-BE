using Demif.Application.Abstractions.Services;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Demif.Infrastructure.Services;

/// <summary>
/// Firebase Storage Service — upload/delete audio files.
/// Bảo mật:
///   - Credential lấy từ Firebase Service Account trong appsettings (không cần env var riêng)
///   - Validate magic bytes để chống file giả mạo content-type
///   - Giới hạn 50MB, whitelist content-type audio
/// </summary>
public class FirebaseStorageService : IFirebaseStorageService
{
    private readonly ILogger<FirebaseStorageService> _logger;
    private readonly string _bucketName;
    private readonly GoogleCredential _credential;
    private StorageClient? _storageClient;
    private readonly object _lock = new();

    private const long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB

    // Magic bytes cho các định dạng audio hợp lệ
    private static readonly Dictionary<string, byte[]> AudioMagicBytes = new()
    {
        // MP3: ID3 header hoặc MPEG frame header
        ["audio/mpeg"] = [0xFF, 0xFB],
        ["audio/mp3"]  = [0xFF, 0xFB],
        // WAV: RIFF header
        ["audio/wav"]  = [0x52, 0x49, 0x46, 0x46], // "RIFF"
        // OGG: OggS header
        ["audio/ogg"]  = [0x4F, 0x67, 0x67, 0x53], // "OggS"
        // M4A: ftyp header (bytes 4-7)
        ["audio/x-m4a"] = [0x66, 0x74, 0x79, 0x70], // "ftyp"
    };

    private static readonly string[] AllowedContentTypes =
        ["audio/mpeg", "audio/mp3", "audio/wav", "audio/ogg", "audio/x-m4a"];

    public FirebaseStorageService(IConfiguration configuration, ILogger<FirebaseStorageService> logger)
    {
        _logger = logger;
        _bucketName = configuration["FirebaseStorage:BucketName"]
            ?? $"{configuration["Firebase:project_id"]}.appspot.com";

        // Tái sử dụng Service Account JSON đã có trong appsettings
        // Không cần set GOOGLE_APPLICATION_CREDENTIALS env var riêng
        _credential = BuildCredentialFromConfig(configuration);
    }

    private static GoogleCredential BuildCredentialFromConfig(IConfiguration configuration)
    {
        var firebaseConfig = configuration.GetSection("Firebase").Get<Dictionary<string, string>>();
        if (firebaseConfig == null || !firebaseConfig.ContainsKey("private_key"))
            throw new InvalidOperationException("Firebase configuration missing. Check appsettings.json.");

        // Xử lý private_key escape (giống FirebaseAuthService)
        var privateKey = firebaseConfig["private_key"]
            .Replace("\\\\n", "\n")
            .Replace("\\n", "\n");

        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            type = firebaseConfig.GetValueOrDefault("type", "service_account"),
            project_id = firebaseConfig.GetValueOrDefault("project_id"),
            private_key_id = firebaseConfig.GetValueOrDefault("private_key_id"),
            private_key = privateKey,
            client_email = firebaseConfig.GetValueOrDefault("client_email"),
            client_id = firebaseConfig.GetValueOrDefault("client_id"),
            auth_uri = firebaseConfig.GetValueOrDefault("auth_uri"),
            token_uri = firebaseConfig.GetValueOrDefault("token_uri"),
        });

        return GoogleCredential.FromJson(json)
            .CreateScoped("https://www.googleapis.com/auth/devstorage.read_write");
    }

    private StorageClient GetClient()
    {
        if (_storageClient != null) return _storageClient;
        lock (_lock)
        {
            if (_storageClient != null) return _storageClient;
            _storageClient = StorageClient.Create(_credential);
            _logger.LogInformation("FirebaseStorageService initialized. Bucket: {Bucket}", _bucketName);
        }
        return _storageClient;
    }

    public async Task<string> UploadAudioAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string lessonId,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate content-type header
        var ct = contentType.ToLower().Split(';')[0].Trim();
        if (!AllowedContentTypes.Contains(ct))
            throw new ArgumentException($"Content-type '{ct}' không hợp lệ. Chỉ chấp nhận: {string.Join(", ", AllowedContentTypes)}");

        // 2. Validate file size
        if (fileStream.Length > MaxFileSizeBytes)
            throw new ArgumentException($"File quá lớn: {fileStream.Length / 1024 / 1024}MB. Tối đa 50MB.");

        if (fileStream.Length == 0)
            throw new ArgumentException("File rỗng.");

        // 3. Validate magic bytes (chống giả mạo content-type)
        await ValidateMagicBytesAsync(fileStream, ct);
        fileStream.Seek(0, SeekOrigin.Begin); // Reset stream sau khi đọc magic bytes

        // 4. Tạo object name an toàn
        var sanitizedName = SanitizeFileName(fileName);
        var objectName = $"lessons/audio/{lessonId}/{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{sanitizedName}";

        _logger.LogInformation("Uploading audio: {ObjectName} ({Size}KB)", objectName, fileStream.Length / 1024);

        try
        {
            var client = GetClient();
            var uploadOptions = new UploadObjectOptions
            {
                PredefinedAcl = PredefinedObjectAcl.PublicRead
            };

            await client.UploadObjectAsync(
                _bucketName, objectName, ct, fileStream, uploadOptions,
                cancellationToken: cancellationToken);

            var publicUrl = $"https://storage.googleapis.com/{_bucketName}/{objectName}";
            _logger.LogInformation("Audio uploaded: {Url}", publicUrl);
            return publicUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload failed: {FileName}", fileName);
            throw;
        }
    }

    public async Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return;

        var prefix = $"https://storage.googleapis.com/{_bucketName}/";
        if (!fileUrl.StartsWith(prefix)) return;

        var objectName = fileUrl[prefix.Length..];

        try
        {
            var client = GetClient();
            await client.DeleteObjectAsync(_bucketName, objectName, cancellationToken: cancellationToken);
            _logger.LogInformation("Deleted: {ObjectName}", objectName);
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("File not found when deleting: {Url}", fileUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed: {Url}", fileUrl);
        }
    }

    /// <summary>
    /// Đọc magic bytes đầu file để xác minh định dạng thật.
    /// Chống FE gửi file .exe đổi tên thành .mp3
    /// </summary>
    private static async Task ValidateMagicBytesAsync(Stream stream, string contentType)
    {
        // MP3 có thể bắt đầu bằng ID3 tag (0x49 0x44 0x33) hoặc MPEG frame (0xFF 0xFB/0xFA/0xF3)
        // M4A: 4 bytes offset trước header "ftyp"
        int headerSize = contentType is "audio/x-m4a" ? 8 : 4;
        var header = new byte[headerSize];
        var read = await stream.ReadAsync(header.AsMemory(0, headerSize));

        if (read < 2)
            throw new ArgumentException("File không hợp lệ — không đọc được header.");

        bool isValid = contentType switch
        {
            "audio/mpeg" or "audio/mp3" =>
                // ID3 tag header: "ID3"
                (header[0] == 0x49 && header[1] == 0x44 && header[2] == 0x33) ||
                // MPEG frame sync: 0xFF 0xFB/0xFA/0xF3/0xF2/0xE3/0xE2
                (header[0] == 0xFF && (header[1] & 0xE0) == 0xE0),

            "audio/wav" =>
                header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46, // "RIFF"

            "audio/ogg" =>
                header[0] == 0x4F && header[1] == 0x67 && header[2] == 0x67 && header[3] == 0x53, // "OggS"

            "audio/x-m4a" =>
                // bytes 4-7 phải là "ftyp"
                read >= 8 &&
                header[4] == 0x66 && header[5] == 0x74 && header[6] == 0x79 && header[7] == 0x70, // "ftyp"

            _ => false
        };

        if (!isValid)
            throw new ArgumentException($"Nội dung file không khớp với định dạng '{contentType}'. Vui lòng upload đúng file audio.");
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", "_")
            .Replace("..", "")
            .Replace("/", "")
            .Replace("\\", "");
        var ext = Path.GetExtension(fileName).ToLower();
        // Chỉ chấp nhận extension audio hợp lệ
        var validExts = new[] { ".mp3", ".wav", ".ogg", ".m4a" };
        if (!validExts.Contains(ext)) ext = ".mp3";
        return $"{name[..Math.Min(name.Length, 50)]}{ext}";
    }
}
