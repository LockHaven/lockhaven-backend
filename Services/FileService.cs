using System.Security.Cryptography;
using Humanizer;
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

    public FileService(ApplicationDbContext dbContext, IBlobStorageService blobStorageService)
    {
        _dbContext = dbContext;
        _blobStorageService = blobStorageService;
    }

    public async Task<File> UploadFile(Stream fileStream, string fileName, string contentType, long fileSize, string userId)
    {
        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(userId))
        {
            throw new ArgumentNullException($"{nameof(fileName)} and {nameof(userId)} cannot be null or empty");
        }

        var extension = Path.GetExtension(fileName)?.TrimStart('.').ToLowerInvariant();
        if (!IsAllowedFileType(extension))
        {
            throw new BadHttpRequestException($"Unsupported file type: {extension}");
        }

        var blobPath = $"users/{userId}/files/{Guid.NewGuid()}.{extension}";

        var file = new File
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

        if (!file.IsClientEncrypted)
        {
            // Server-side encryption (current approach)
            var key = GenerateEncryptionKey();
            var iv = GenerateInitializationVector();
            file.EncryptedKey = Convert.ToBase64String(key);
            file.InitializationVector = Convert.ToBase64String(iv);
            
            // Encrypt the file stream
            fileStream = EncryptStream(fileStream, key, iv);
        }
        else
        {
            // Client-side encryption (future approach)
            // File is already encrypted, just store it
            file.EncryptedKey = string.Empty;
            file.InitializationVector = string.Empty;
        }

        // Store in blob storage
        await _blobStorageService.UploadAsync(fileStream, blobPath, contentType);
        
        // Save to database
        _dbContext.Files.Add(file);
        await _dbContext.SaveChangesAsync();
        
        return file;
    }

    public async Task<Stream> DownloadFile(string fileId, string userId)
    {
        if (string.IsNullOrEmpty(fileId) || string.IsNullOrEmpty(userId))
        {
            throw new ArgumentNullException("fileId and userId cannot be null or empty");
        }

        // Get file metadata and verify ownership
        var file = await GetFileById(fileId, userId) ?? throw new FileNotFoundException($"File with id {fileId} not found or access denied");

        // Download from blob storage
        var encryptedStream = await _blobStorageService.DownloadAsync(file.BlobPath);

        if (!file.IsClientEncrypted)
        {
            // Server-side encryption - decrypt the stream
            var key = Convert.FromBase64String(file.EncryptedKey);
            var iv = Convert.FromBase64String(file.InitializationVector);
            return DecryptStream(encryptedStream, key, iv);
        }
        else
        {
            // Client-side encryption - return as-is (client will decrypt)
            return encryptedStream;
        }
    }

    public async Task<File?> GetFileById(string fileId, string userId)
    {
        if (string.IsNullOrEmpty(fileId) || string.IsNullOrEmpty(userId)) 
        {
            throw new ArgumentNullException($"{nameof(fileId)} and {nameof(userId)} cannot be null or empty");
        }

        var file = await _dbContext.Files
            .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId) ?? throw new FileNotFoundException($"File with id {fileId} not found or access denied");

        return file;
    }

    public async Task<ICollection<File>> GetUserFiles(string userId)
    {
        if (string.IsNullOrEmpty(userId)) 
        {
            throw new ArgumentNullException($"{nameof(userId)} cannot be null or empty");
        }

        return await _dbContext.Files.Where(f => f.UserId == userId).ToListAsync();
    }

    public async Task<bool> DeleteFile(string fileId, string userId)
    {
        if (string.IsNullOrEmpty(fileId) || string.IsNullOrEmpty(userId))
        {
            throw new ArgumentNullException($"{nameof(fileId)} and {nameof(userId)} cannot be null or empty");
        }

        var file = await _dbContext.Files
            .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId) 
            ?? throw new FileNotFoundException($"File with id {fileId} not found or access denied");

        await _blobStorageService.DeleteAsync(file.BlobPath);

        _dbContext.Files.Remove(file);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<long> GetUserStorageUsed(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            throw new ArgumentNullException($"{nameof(userId)} cannot be null or empty");
        }

        return await _dbContext.Files
            .Where(f => f.UserId == userId)
            .SumAsync(f => f.Size);
    }

    public bool IsFileTypeAllowed(string fileType)
    {
        if (string.IsNullOrEmpty(fileType))
        {
            return false;
        }

        return AcceptedFileTypes.AllowedFileTypes.Contains(fileType);
    }

    private async Task<bool> UserOwnsFile(string fileId, string userId)
    {
        if (string.IsNullOrEmpty(fileId) || string.IsNullOrEmpty(userId)) 
        {
            throw new ArgumentNullException($"{nameof(fileId)} and {nameof(userId)} cannot be null or empty");
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
            ".zip" => FileType.Zip,
            _ => FileType.Txt // Default fallback
        };
    }

    /// <summary>
    /// Encrypts a stream using AES-256-GCM
    /// </summary>
    private Stream EncryptStream(Stream inputStream, byte[] key, byte[] iv)
    {
        using var aesGcm = new AesGcm(key, EncryptionConstants.TagSize);
        
        // Read the entire input stream into memory
        using var memoryStream = new MemoryStream();
        inputStream.CopyTo(memoryStream);
        var plaintext = memoryStream.ToArray();
        
        // Create arrays for ciphertext and tag
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[EncryptionConstants.TagSize];
        
        // Encrypt using the correct method signature
        aesGcm.Encrypt(iv, plaintext, ciphertext, tag);
        
        // Combine ciphertext and tag into a single stream
        var result = new MemoryStream();
        result.Write(ciphertext, 0, ciphertext.Length);
        result.Write(tag, 0, tag.Length);
        result.Position = 0;
        
        return result;
    }

    /// <summary>
    /// Decrypts a stream using AES-256-GCM
    /// </summary>
    private Stream DecryptStream(Stream encryptedStream, byte[] key, byte[] iv)
    {
        using var aesGcm = new AesGcm(key, EncryptionConstants.TagSize);

        // Read the encrypted stream
        using var memoryStream = new MemoryStream();
        encryptedStream.CopyTo(memoryStream);
        var encryptedData = memoryStream.ToArray();

        // Split ciphertext and tag
        var tagSize = EncryptionConstants.TagSize;
        var ciphertextLength = encryptedData.Length - tagSize;

        var ciphertext = new byte[ciphertextLength];
        var tag = new byte[tagSize];

        Array.Copy(encryptedData, 0, ciphertext, 0, ciphertextLength);
        Array.Copy(encryptedData, ciphertextLength, tag, 0, tagSize);

        // Decrypt
        var plaintext = new byte[ciphertextLength];
        aesGcm.Decrypt(iv, ciphertext, tag, plaintext);

        return new MemoryStream(plaintext);
    }
    
    private bool IsAllowedFileType(string extension)
        => !string.IsNullOrEmpty(extension)
            && AcceptedFileTypes.AllowedFileTypes.Contains(extension);
}   