using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Demif.Application.Abstractions.Services
{
    public interface IImageUploadService
    {
        Task<string?> UploadImageAsync(IFormFile file, string folderName);
    }
}