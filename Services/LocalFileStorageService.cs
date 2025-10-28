using lockhaven_backend.Services.Interfaces;

namespace lockhaven_backend.Services;

public class LocalFileStorageService : IBlobStorageService
{
    private readonly string _storagePath;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "local-storage");
        _logger = logger;
        
        // Ensure storage directory exists
        Directory.CreateDirectory(_storagePath);
    }

    public async Task<string> UploadAsync(
        Stream content, 
        string blobPath, 
        string contentType, 
        IDictionary<string, string>? metadata = null, 
        bool overwrite = false, 
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_storagePath, blobPath);
        var directory = Path.GetDirectoryName(fullPath);
        
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var fileStream = new FileStream(fullPath, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write);
        await content.CopyToAsync(fileStream, cancellationToken);
        
        _logger.LogInformation("File uploaded to local storage: {Path}", fullPath);
        return blobPath;
    }

    public async Task<Stream> DownloadAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_storagePath, blobPath);
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {blobPath}");
        }

        var memoryStream = new MemoryStream();
        using var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        await fileStream.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;
        
        return memoryStream;
    }

    public async Task<bool> DeleteAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_storagePath, blobPath);
        
        if (!File.Exists(fullPath))
        {
            return false;
        }

        File.Delete(fullPath);
        _logger.LogInformation("File deleted from local storage: {Path}", fullPath);
        return true;
    }

    public async Task<bool> ExistsAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        var fullPath = Path.Combine(_storagePath, blobPath);
        return File.Exists(fullPath);
    }
}