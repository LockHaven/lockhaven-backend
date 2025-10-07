using File = lockhaven_backend.Models.File;

namespace lockhaven_backend.Services.Interfaces;

public interface IFileService
{
    Task<File> UploadFile(Stream fileStream, string fileName, string contentType, long fileSize, string userId);
    Task<Stream> DownloadFile(string fileId, string userId);
    Task<File?> GetFileById(string fileId, string userId);
    Task<ICollection<File>> GetUserFiles(string userId);
    Task<bool> DeleteFile(string fileId, string userId);
    Task<bool> UserOwnsFile(string fileId, string userId);
    Task<long> GetUserStorageUsed(string userId);
    bool IsFileTypeAllowed(string fileType);
}