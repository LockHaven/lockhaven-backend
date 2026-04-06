using System.Data;
using System.Security.Cryptography;
using lockhaven_backend.Constants;
using lockhaven_backend.Data;
using lockhaven_backend.Models;
using lockhaven_backend.Services.Interfaces;
using lockhaven_backend.Services.Streaming;
using Microsoft.EntityFrameworkCore;
using File = lockhaven_backend.Models.File;

namespace lockhaven_backend.Services;

public class FileService : IFileService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IBlobStorageService _blobStorageService;
    private readonly IKeyEncryptionService _keyEncryptionService;
    private readonly IFileValidationService _fileValidationService;

    public FileService(
        ApplicationDbContext dbContext,
        IBlobStorageService blobStorageService,
        IKeyEncryptionService keyEncryptionService,
        IFileValidationService fileValidationService)
    {
        _dbContext = dbContext;
        _blobStorageService = blobStorageService;
        _keyEncryptionService = keyEncryptionService;
        _fileValidationService = fileValidationService;
    }

    public async Task<File> UploadFile(IFormFile file, Guid userId, CancellationToken cancellationToken = default)
    {
        if (Guid.Empty == userId)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        _fileValidationService.ValidateUpload(file);

        var fileName = file.FileName;
        var fileSize = file.Length;
        var contentType = file.ContentType;
        var extension = Path.GetExtension(fileName)?.TrimStart('.').ToLowerInvariant() ?? string.Empty;

        var blobPath = $"users/{userId}/files/{Guid.NewGuid()}.{extension}";
        using var fileStream = file.OpenReadStream();
        Stream uploadStream = fileStream;

        var fileEntity = new File
        {
            Name = fileName,
            Type = GetFileTypeFromExtension($".{extension}"),
            Size = fileSize,
            ContentType = contentType,
            BlobPath = blobPath,
            UserId = userId,
            IsClientEncrypted = false, // Will be true for client-side encryption
            IsShared = false,
            GroupId = null
        };

        if (!fileEntity.IsClientEncrypted)
        {
            // Server-side encryption with envelope encryption
            // Generate per-file data encryption key (DEK) and IV
            var key = GenerateEncryptionKey();
            var iv = GenerateInitializationVector();

            // Encrypt the DEK and IV with the KEK (e.g. Vault Transit)
            fileEntity.EncryptedKey = await _keyEncryptionService.EncryptKeyAsync(key, cancellationToken);
            fileEntity.InitializationVector = await _keyEncryptionService.EncryptIvAsync(iv, cancellationToken);

            // Encrypt on read while uploading — does not buffer the whole ciphertext in RAM
            uploadStream = new ChunkedAesGcmEncryptingStream(fileStream, key, iv, leavePlaintextOpen: true);
        }
        else
        {
            // Client-side encryption (future approach)
            fileEntity.EncryptedKey = string.Empty;
            fileEntity.InitializationVector = string.Empty;
        }

        // Serializable transaction serializes quota checks + inserts so concurrent uploads cannot bypass limits.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new UnauthorizedAccessException("User not found");

            await RefreshUserUsageMetrics(user);
            EnforceTierLimits(user, fileSize);

            _dbContext.Files.Add(fileEntity);

            user.CurrentStorageUsedBytes += fileSize;
            user.UploadsTodayCount += 1;
            user.UploadsCountDateUtc = DateTime.UtcNow.Date;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        try
        {
            await _blobStorageService.UploadAsync(uploadStream, blobPath, contentType, cancellationToken: cancellationToken);
        }
        catch
        {
            await CompensateFailedBlobUploadAsync(fileEntity.Id, userId);
            throw;
        }
        finally
        {
            if (!ReferenceEquals(uploadStream, fileStream))
            {
                uploadStream.Dispose();
            }
        }

        return fileEntity;
    }

    private async Task CompensateFailedBlobUploadAsync(Guid fileId, Guid userId)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var fileEntity = await _dbContext.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);

            if (fileEntity == null)
            {
                await transaction.CommitAsync();
                return;
            }

            _dbContext.Files.Remove(fileEntity);

            var user = await _dbContext.Users.FirstAsync(u => u.Id == userId);
            await RefreshUserUsageMetrics(user);
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Stream> DownloadFile(Guid fileId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (fileId == Guid.Empty || userId == Guid.Empty)
        {
            throw new ArgumentNullException("fileId and userId cannot be empty");
        }

        // Get file metadata and verify ownership
        var file = await GetFileById(fileId, userId) ?? throw new FileNotFoundException($"File with id {fileId} not found or access denied");

        // Download from blob storage
        var encryptedStream = await _blobStorageService.DownloadAsync(file.BlobPath, cancellationToken);

        if (!file.IsClientEncrypted)
        {
            // Server-side encryption - decrypt the DEK and IV using the KEK (e.g. Vault Transit)
            var key = await _keyEncryptionService.DecryptKeyAsync(file.EncryptedKey, cancellationToken);
            var iv = await _keyEncryptionService.DecryptIvAsync(file.InitializationVector, cancellationToken);
            
            // Decrypt on read — does not buffer the whole plaintext in RAM
            return new ChunkedAesGcmDecryptingStream(encryptedStream, key, iv, leaveCiphertextOpen: false);
        }
        else
        {
            // Client-side encryption - return as-is (client will decrypt)
            return encryptedStream;
        }
    }

    public async Task<File?> GetFileById(Guid fileId, Guid userId)
    {
        if (fileId == Guid.Empty || userId == Guid.Empty) 
        {
            throw new ArgumentNullException($"{nameof(fileId)} and {nameof(userId)} cannot be empty");
        }

        var file = await _dbContext.Files
            .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId) ?? throw new FileNotFoundException($"File with id {fileId} not found or access denied");

        return file;
    }

    public async Task<ICollection<File>> GetUserFiles(Guid userId)
    {
        if (userId == Guid.Empty) 
        {
            throw new ArgumentNullException($"{nameof(userId)} cannot be empty");
        }

        return await _dbContext.Files.Where(f => f.UserId == userId).ToListAsync();
    }

    public async Task<bool> DeleteFile(Guid fileId, Guid userId)
    {
        if (fileId == Guid.Empty || userId == Guid.Empty)
        {
            throw new ArgumentNullException($"{nameof(fileId)} and {nameof(userId)} cannot be empty");
        }

        var file = await _dbContext.Files
            .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId) 
            ?? throw new FileNotFoundException($"File with id {fileId} not found or access denied");

        await _blobStorageService.DeleteAsync(file.BlobPath);

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new UnauthorizedAccessException("User not found");

        await RefreshUserUsageMetrics(user);
        user.CurrentStorageUsedBytes = Math.Max(0, user.CurrentStorageUsedBytes - file.Size);
        user.UpdatedAt = DateTime.UtcNow;

        _dbContext.Files.Remove(file);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<long> GetUserStorageUsed(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentNullException($"{nameof(userId)} cannot be empty");
        }

        return await _dbContext.Files
            .Where(f => f.UserId == userId)
            .SumAsync(f => f.Size);
    }

    private async Task<bool> UserOwnsFile(Guid fileId, Guid userId)
    {
        if (fileId == Guid.Empty || userId == Guid.Empty) 
        {
            throw new ArgumentNullException($"{nameof(fileId)} and {nameof(userId)} cannot be empty");
        }

        return await _dbContext.Files
            .AnyAsync(f => f.Id == fileId && f.UserId == userId);
    }

    /// <summary>
    /// Generates a cryptographically secure 256-bit encryption key for AES-256-GCM
    /// </summary>
    /// <returns>A 32-byte encryption key</returns>
    private byte[] GenerateEncryptionKey()
    {   
        return RandomNumberGenerator.GetBytes(32);
    }

    /// <summary>
    /// Generates a cryptographically secure initialization vector for AES-256-GCM
    /// </summary>
    /// <returns>A 12-byte initialization vector</returns>
    private byte[] GenerateInitializationVector()
    {
        return RandomNumberGenerator.GetBytes(12);
    }

    /// <summary>
    /// Determines FileType enum from file extension
    /// </summary>
    private FileType GetFileTypeFromExtension(string extension)
    {
        return extension.ToLower() switch
        {
            ".pdf" => FileType.Pdf,
            ".doc" => FileType.Doc,
            ".docx" => FileType.Docx,
            ".txt" => FileType.Txt,
            ".xlsx" => FileType.Xlsx,
            ".xls" => FileType.Xls,
            ".csv" => FileType.Csv,
            ".jpg" or ".jpeg" => FileType.Jpg,
            ".png" => FileType.Png,
            ".gif" => FileType.Gif,
            ".bmp" => FileType.Bmp,
            ".mp4" => FileType.Mp4,
            ".mp3" => FileType.Mp3,
            ".wav" => FileType.Wav,
            ".json" => FileType.Json,
            ".xml" => FileType.Xml,
            //".zip" => FileType.Zip, NOTE: Disabled due to security concerns for now
            _ => FileType.Txt // Default fallback
        };
    }

    private static string FormatBytes(long bytes)
    {
        const long kb = 1024;
        const long mb = kb * 1024;
        const long gb = mb * 1024;

        if (bytes >= gb) return $"{bytes / (double)gb:0.##} GB";
        if (bytes >= mb) return $"{bytes / (double)mb:0.##} MB";
        if (bytes >= kb) return $"{bytes / (double)kb:0.##} KB";
        return $"{bytes} bytes";
    }

    private void EnforceTierLimits(User user, long incomingFileSize)
    {
        var limits = SubscriptionLimits.ForTier(user.SubscriptionTier);

        if (incomingFileSize > limits.MaxFileSizeBytes)
        {
            throw new BadHttpRequestException(
                $"This file exceeds your {user.SubscriptionTier} tier max file size of {FormatBytes(limits.MaxFileSizeBytes)}.");
        }

        if (user.CurrentStorageUsedBytes + incomingFileSize > limits.MaxTotalStorageBytes)
        {
            throw new BadHttpRequestException(
                $"This upload would exceed your {user.SubscriptionTier} tier storage limit of {FormatBytes(limits.MaxTotalStorageBytes)}.");
        }

        if (limits.MaxUploadsPerDay.HasValue && user.UploadsTodayCount >= limits.MaxUploadsPerDay.Value)
        {
            throw new BadHttpRequestException(
                $"You have reached your {user.SubscriptionTier} tier daily upload limit ({limits.MaxUploadsPerDay.Value}/day).");
        }
    }

    private async Task RefreshUserUsageMetrics(User user)
    {
        user.CurrentStorageUsedBytes = await _dbContext.Files
            .Where(f => f.UserId == user.Id)
            .SumAsync(f => (long?)f.Size) ?? 0;

        var startOfTodayUtc = DateTime.UtcNow.Date;
        var endOfTodayUtc = startOfTodayUtc.AddDays(1);

        user.UploadsTodayCount = await _dbContext.Files
            .CountAsync(f =>
                f.UserId == user.Id &&
                f.UploadedAt >= startOfTodayUtc &&
                f.UploadedAt < endOfTodayUtc);

        user.UploadsCountDateUtc = startOfTodayUtc;
    }
}   