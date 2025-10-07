namespace lockhaven_backend.Services.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(
        Stream content,
        string blobPath,
        string contentType,
        IDictionary<string, string>? metadata = null,
        bool overwrite = false,
        CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(
        string blobPath,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string blobPath,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string blobPath,
        CancellationToken cancellationToken = default);

}