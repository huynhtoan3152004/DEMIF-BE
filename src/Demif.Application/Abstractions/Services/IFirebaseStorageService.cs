namespace Demif.Application.Abstractions.Services;

/// <summary>
/// Interface cho Firebase Storage — upload/delete files
/// </summary>
public interface IFirebaseStorageService
{
    /// <summary>
    /// Upload audio file lên Firebase Storage.
    /// Trả về public download URL.
    /// </summary>
    Task<string> UploadAudioAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        string lessonId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa file theo URL đầy đủ (cleanup khi update/delete lesson)
    /// </summary>
    Task DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
}
