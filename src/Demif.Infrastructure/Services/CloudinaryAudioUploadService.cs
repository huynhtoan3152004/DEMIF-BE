using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Demif.Application.Abstractions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Demif.Infrastructure.Services;

public class CloudinaryAudioUploadService : IAudioUploadService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryAudioUploadService> _logger;

    public CloudinaryAudioUploadService(IConfiguration config, ILogger<CloudinaryAudioUploadService> logger)
    {
        _logger = logger;

        var account = new Account(
            config["Cloudinary:CloudName"],
            config["Cloudinary:ApiKey"],
            config["Cloudinary:ApiSecret"]);

        _cloudinary = new Cloudinary(account);
    }

    public async Task<string?> UploadAudioAsync(IFormFile file, string folderName)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogWarning("[Cloudinary] UploadAudioAsync called with null/empty file");
            return null;
        }

        _logger.LogInformation(
            "[Cloudinary] Starting upload: FileName={FileName}, Size={Size}b, ContentType={ContentType}, Folder={Folder}",
            file.FileName, file.Length, file.ContentType, folderName);

        await using var stream = file.OpenReadStream();
        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folderName
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            _logger.LogError("[Cloudinary] Upload failed: {Error}", uploadResult.Error.Message);
            throw new Exception($"Lỗi upload audio: {uploadResult.Error.Message}");
        }

        var url = uploadResult.SecureUrl?.ToString() ?? uploadResult.Url?.ToString();
        _logger.LogInformation("[Cloudinary] Upload success: URL={Url}, PublicId={PublicId}", url, uploadResult.PublicId);

        return url;
    }
}