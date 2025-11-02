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

    public FileService(ApplicationDbContext dbContext, IBlobStorageService blobStorageService, IKeyEncryptionService keyEncryptionService)
    {
        _dbContext = dbContext;
        _blobStorageService = blobStorageService;
        _keyEncryptionService = keyEncryptionService;
    }

    public async Task<File> UploadFile(Stream fileStream, string fileName, string contentType, long fileSize, string userId)
    {
        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(userId))
        {
            throw new ArgumentNullException($"{nameof(fileName)} and {nameof(userId)} cannot be null or empty");
        }

        var extension = Path.GetExtension(fileName)?.TrimStart('.').ToLowerInvariant() ?? string.Empty;
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
            // Server-side encryption with envelope encryption
            // Generate per-file data encryption key (DEK) and IV
            var key = GenerateEncryptionKey();
            var iv = GenerateInitializationVector();
            
            // Encrypt the DEK and IV with the Key Encryption Key (KEK) from Azure Key Vault
            file.EncryptedKey = await _keyEncryptionService.EncryptKeyAsync(key);
            file.InitializationVector = await _keyEncryptionService.EncryptIvAsync(iv);
            
            // Encrypt the file stream with the DEK using chunked format
            fileStream = EncryptStreamChunked(fileStream, key, iv);
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

    /// <summary>
    /// Encrypts a stream using legacy single-IV AES-256-GCM (Format V1 - deprecated)
    /// Kept for backward compatibility with existing files
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
    
    private bool IsAllowedFileType(string extension)
        => !string.IsNullOrEmpty(extension)
            && AcceptedFileTypes.AllowedFileTypes.Contains(extension);
}   