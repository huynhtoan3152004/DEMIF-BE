using Demif.Application.Abstractions.Services;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Demif.Infrastructure.Services;

/// <summary>
/// Firebase Storage Service — upload/delete MP3 files.
/// Dùng Google.Cloud.Storage.V1 với credential từ Firebase Service Account.
/// </summary>
public class FirebaseStorageService : IFirebaseStorageService
{
    private readonly ILogger<FirebaseStorageService> _logger;
    private readonly string _bucketName;
    private StorageClient? _storageClient;
    private readonly object _lock = new();

    // 50MB max upload size
    private const long MaxFileSizeBytes = 50 * 1024 * 1024;

    private static readonly string[] AllowedContentTypes =
        ["audio/mpeg", "audio/mp3", "audio/wav", "audio/ogg", "audio/x-m4a"];

    public FirebaseStorageService(IConfiguration configuration, ILogger<FirebaseStorageService> logger)
    {
        _logger = logger;
        _bucketName = configuration["FirebaseStorage:BucketName"]
            ?? $"{configuration["Firebase:project_id"]}.appspot.com";
    }

    /// <summary>
    /// Lazy-init StorageClient dùng chính Service Account của Firebase.
    /// Tái sử dụng credential từ FirebaseAuthService đã setup.
    /// </summary>
    private StorageClient GetClient()
    {
        if (_storageClient != null) return _storageClient;

        lock (_lock)
        {
            if (_storageClient != null) return _storageClient;

            // Tái sử dụng Application Default Credentials (ADC) từ GOOGLE_APPLICATION_CREDENTIALS
            // hoặc dùng credential từ environment
            _storageClient = StorageClient.Create();
            _logger.LogInformation("FirebaseStorageService initialized with bucket: {Bucket}", _bucketName);
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
        // Validate content type
        if (!AllowedContentTypes.Contains(contentType.ToLower()))
            throw new ArgumentException($"Content type '{contentType}' không hợp lệ. Chỉ chấp nhận: {string.Join(", ", AllowedContentTypes)}");

        // Validate file size
        if (fileStream.Length > MaxFileSizeBytes)
            throw new ArgumentException($"File quá lớn. Tối đa 50MB, file hiện tại: {fileStream.Length / 1024 / 1024}MB");

        // Tạo tên object trong Storage: lessons/audio/{lessonId}/{timestamp}_{filename}
        var sanitizedName = SanitizeFileName(fileName);
        var objectName = $"lessons/audio/{lessonId}/{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{sanitizedName}";

        _logger.LogInformation("Uploading audio file: {ObjectName} to bucket: {Bucket}", objectName, _bucketName);

        try
        {
            var client = GetClient();

            var uploadOptions = new UploadObjectOptions
            {
                PredefinedAcl = PredefinedObjectAcl.PublicRead // Cho phép đọc public — cần để stream audio
            };

            var result = await client.UploadObjectAsync(
                _bucketName,
                objectName,
                contentType,
                fileStream,
                uploadOptions,
                cancellationToken: cancellationToken);

            // Tạo public URL theo format chuẩn của Google Cloud Storage
            var publicUrl = $"https://storage.googleapis.com/{_bucketName}/{objectName}";

            _logger.LogInformation("Audio uploaded successfully: {Url}", publicUrl);
            return publicUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload audio file: {FileName}", fileName);
            throw;
        }
    }

    public async Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl)) return;

        try
        {
            // Extract objectName từ URL: https://storage.googleapis.com/{bucket}/{objectName}
            var prefix = $"https://storage.googleapis.com/{_bucketName}/";
            if (!fileUrl.StartsWith(prefix)) return;

            var objectName = fileUrl[prefix.Length..];
            var client = GetClient();

            await client.DeleteObjectAsync(_bucketName, objectName, cancellationToken: cancellationToken);
            _logger.LogInformation("Deleted audio file: {ObjectName}", objectName);
        }
        catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("File not found when deleting: {Url}", fileUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete audio file: {Url}", fileUrl);
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        // Chỉ giữ ký tự an toàn cho object name
        var name = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", "_")
            .Replace("..", "");
        var ext = Path.GetExtension(fileName).ToLower();
        return $"{name}{ext}";
    }
}
