using System.Security.Cryptography;
using lockhaven_backend.Constants;
using lockhaven_backend.Data;
using lockhaven_backend.Models;
using lockhaven_backend.Services.Interfaces;
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

    public async Task<File> UploadFile(IFormFile file, Guid userId)
    {
        if (Guid.Empty == userId)
        {
            throw new ArgumentNullException(nameof(userId));
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new UnauthorizedAccessException("User not found");

        await RefreshUserUsageMetrics(user);
        EnforceTierLimits(user, file.Length);

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
            
            // Encrypt the DEK and IV with the Key Encryption Key (KEK) from Azure Key Vault
            fileEntity.EncryptedKey = await _keyEncryptionService.EncryptKeyAsync(key);
            fileEntity.InitializationVector = await _keyEncryptionService.EncryptIvAsync(iv);
            
            // Encrypt the file stream with the DEK using chunked format
            uploadStream = EncryptStreamChunked(fileStream, key, iv);
        }
        else
        {
            // Client-side encryption (future approach)
            // File is already encrypted, just store it
            fileEntity.EncryptedKey = string.Empty;
            fileEntity.InitializationVector = string.Empty;
        }

        try
        {
            // Store in blob storage
            await _blobStorageService.UploadAsync(uploadStream, blobPath, contentType);
        }
        finally
        {
            if (!ReferenceEquals(uploadStream, fileStream))
            {
                uploadStream.Dispose();
            }
        }
        
        // Save to database
        _dbContext.Files.Add(fileEntity);

        user.CurrentStorageUsedBytes += fileSize;
        user.UploadsTodayCount += 1;
        user.UploadsCountDateUtc = DateTime.UtcNow.Date;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        
        return fileEntity;
    }

    public async Task<Stream> DownloadFile(Guid fileId, Guid userId)
    {
        if (fileId == Guid.Empty || userId == Guid.Empty)
        {
            throw new ArgumentNullException("fileId and userId cannot be empty");
        }

        // Get file metadata and verify ownership
        var file = await GetFileById(fileId, userId) ?? throw new FileNotFoundException($"File with id {fileId} not found or access denied");

        // Download from blob storage
        var encryptedStream = await _blobStorageService.DownloadAsync(file.BlobPath);

        if (!file.IsClientEncrypted)
        {
            // Server-side encryption - decrypt the DEK and IV using the KEK from Azure Key Vault
            var key = await _keyEncryptionService.DecryptKeyAsync(file.EncryptedKey);
            var iv = await _keyEncryptionService.DecryptIvAsync(file.InitializationVector);
            
            // Decrypt the file stream with the decrypted DEK
            return DecryptStreamChunked(encryptedStream, key, iv);
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

    /// <summary>
    /// Encrypts a stream using chunked AES-256-GCM (Format V2)
    /// Format: [Chunk1: [ChunkIV(12)][Tag(16)][CiphertextLength(4)][Ciphertext(N)]][Chunk2:...]
    /// Base IV is stored in database, each chunk gets unique IV derived from base IV + counter
    /// </summary>
    private Stream EncryptStreamChunked(Stream inputStream, byte[] key, byte[] baseIv)
    {
        using var aesGcm = new AesGcm(key, EncryptionConstants.TagSize);
        
        var result = new MemoryStream();
        var buffer = new byte[EncryptionConstants.ChunkSize];
        var chunkIv = new byte[EncryptionConstants.NonceSize];
        var chunkCounter = 0;
        
        int bytesRead;
        while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
        {
            // Derive unique IV for this chunk: base IV with counter appended to last 4 bytes
            baseIv.CopyTo(chunkIv, 0);
            var counterBytes = BitConverter.GetBytes(chunkCounter++);
            Array.Copy(counterBytes, 0, chunkIv, EncryptionConstants.NonceSize - 4, 4);
            
            // Encrypt chunk
            var ciphertext = new byte[bytesRead];
            var tag = new byte[EncryptionConstants.TagSize];
            aesGcm.Encrypt(chunkIv, buffer.AsSpan(0, bytesRead), ciphertext, tag);
            
            // Write chunk IV, tag, ciphertext length, and ciphertext
            result.Write(chunkIv, 0, chunkIv.Length);
            result.Write(tag, 0, tag.Length);
            var lengthBytes = BitConverter.GetBytes(ciphertext.Length);
            result.Write(lengthBytes, 0, lengthBytes.Length);
            result.Write(ciphertext, 0, ciphertext.Length);
        }
        
        result.Position = 0;
        return result;
    }

    /// <summary>
    /// Decrypts a chunked AES-256-GCM stream (Format V2)
    /// Format: [Chunk1: [ChunkIV(12)][Tag(16)][CiphertextLength(4)][Ciphertext(N)]][Chunk2:...]
    /// </summary>
    private Stream DecryptStreamChunked(Stream encryptedStream, byte[] key, byte[] baseIv)
    {
        using var aesGcm = new AesGcm(key, EncryptionConstants.TagSize);
        
        var result = new MemoryStream();
        var chunkIv = new byte[EncryptionConstants.NonceSize];
        var tag = new byte[EncryptionConstants.TagSize];
        var lengthBytes = new byte[sizeof(int)];
        var chunkCounter = 0;
        
        while (true)
        {
            // Read chunk IV - if we can't read a full IV, we've reached the end
            var ivRead = ReadExactly(encryptedStream, chunkIv, 0, chunkIv.Length);
            if (ivRead == 0)
            {
                break; // End of stream
            }
            if (ivRead != chunkIv.Length)
            {
                throw new CryptographicException("Invalid encrypted file format: truncated chunk IV");
            }
            
            // Read chunk tag
            var tagRead = ReadExactly(encryptedStream, tag, 0, tag.Length);
            if (tagRead != tag.Length)
            {
                throw new CryptographicException("Invalid encrypted file format: truncated chunk tag");
            }
            
            // Read ciphertext length
            var lengthRead = ReadExactly(encryptedStream, lengthBytes, 0, lengthBytes.Length);
            if (lengthRead != lengthBytes.Length)
            {
                throw new CryptographicException("Invalid encrypted file format: truncated ciphertext length");
            }
            
            var ciphertextLength = BitConverter.ToInt32(lengthBytes, 0);
            if (ciphertextLength < 0 || ciphertextLength > EncryptionConstants.ChunkSize)
            {
                throw new CryptographicException($"Invalid encrypted file format: invalid ciphertext length {ciphertextLength}");
            }
            
            // Read chunk ciphertext - read exactly the specified length
            var ciphertext = new byte[ciphertextLength];
            var ciphertextRead = ReadExactly(encryptedStream, ciphertext, 0, ciphertextLength);
            if (ciphertextRead != ciphertextLength)
            {
                throw new CryptographicException($"Invalid encrypted file format: truncated ciphertext (expected {ciphertextLength}, got {ciphertextRead})");
            }
            
            // Decrypt chunk
            var plaintext = new byte[ciphertextLength];
            aesGcm.Decrypt(chunkIv, ciphertext, tag, plaintext);
            
            // Write decrypted chunk
            result.Write(plaintext, 0, plaintext.Length);
            
            chunkCounter++;
        }
        
        result.Position = 0;
        return result;
    }
    
    /// <summary>
    /// Reads exactly the requested number of bytes from the stream.
    /// This is necessary because Stream.Read doesn't guarantee filling the buffer.
    /// </summary>
    private int ReadExactly(Stream stream, byte[] buffer, int offset, int count)
    {
        int totalRead = 0;
        while (totalRead < count)
        {
            int bytesRead = stream.Read(buffer, offset + totalRead, count - totalRead);
            if (bytesRead == 0)
            {
                // End of stream reached before reading requested amount
                return totalRead;
            }
            totalRead += bytesRead;
        }
        return totalRead;
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