using File = lockhaven_backend.Models.File;
using Microsoft.AspNetCore.Http;

namespace lockhaven_backend.Services.Interfaces;

public interface IFileService
{
    Task<File> UploadFile(IFormFile file, string userId);
    Task<Stream> DownloadFile(string fileId, string userId);
    Task<File?> GetFileById(string fileId, string userId);
    Task<ICollection<File>> GetUserFiles(string userId);
    Task<bool> DeleteFile(string fileId, string userId);
    Task<long> GetUserStorageUsed(string userId);
}