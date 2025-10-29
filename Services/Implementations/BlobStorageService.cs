using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using lockhaven_backend.Services.Interfaces;

namespace lockhaven_backend.Services;

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _container;

    public BlobStorageService(BlobServiceClient blobServiceClient, IConfiguration configuration)
    {
        var containerName = configuration["BlobStorage:ContainerName"]
            ?? throw new InvalidOperationException("Blob storage container name is not configured");
       
        _container = blobServiceClient.GetBlobContainerClient(containerName);
        _container.CreateIfNotExists(PublicAccessType.None);
    }

    public async Task<string> UploadAsync(
        Stream content, 
        string blobPath, 
        string contentType, 
        IDictionary<string, string>? metadata = null, 
        bool overwrite = false, 
        CancellationToken cancellationToken = default)
    {
        var blobClient = _container.GetBlobClient(blobPath);

        var headers = new BlobHttpHeaders { ContentType = contentType };
        await blobClient.UploadAsync(content, new BlobUploadOptions 
        { 
            HttpHeaders = headers,
            Metadata = metadata, 
        }, cancellationToken);

        return blobPath;
    }

    public async Task<Stream> DownloadAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        var blobClient = _container.GetBlobClient(blobPath);
        var ms = new MemoryStream();
        await blobClient.DownloadToAsync(ms, cancellationToken);
        ms.Position = 0;
        return ms;
    }

    public async Task<bool> DeleteAsync(string blobPath, CancellationToken cancellationToken = default)
    {
        var blobClient = _container.GetBlobClient(blobPath);
        var result = await blobClient.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, null, cancellationToken);
        return result.Value;
    }

    public async Task<bool> ExistsAsync(string blobPath, CancellationToken cancellationToken = default) // WILL THIS BE NEEDED?
    {
        var blobClient = _container.GetBlobClient(blobPath);
        var exists = await blobClient.ExistsAsync(cancellationToken);
        return exists.Value;
    }
}