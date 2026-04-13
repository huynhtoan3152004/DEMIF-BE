using Microsoft.AspNetCore.Http;

namespace Demif.Application.Abstractions.Services;

public interface IAudioUploadService
{
    Task<string?> UploadAudioAsync(IFormFile file, string folderName);
}