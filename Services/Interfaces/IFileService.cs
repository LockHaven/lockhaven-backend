using File = lockhaven_backend.Models.File;
using Microsoft.AspNetCore.Http;

namespace lockhaven_backend.Services.Interfaces;

public interface IFileService
{
    Task<File> UploadFile(IFormFile file, Guid userId);
    Task<Stream> DownloadFile(Guid fileId, Guid userId);
    Task<File?> GetFileById(Guid fileId, Guid userId);
    Task<ICollection<File>> GetUserFiles(Guid userId);
    Task<bool> DeleteFile(Guid fileId, Guid userId);
    Task<long> GetUserStorageUsed(Guid userId);
}