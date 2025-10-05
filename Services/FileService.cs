using System.Security.Cryptography;
using lockhaven_backend.Constants;
using lockhaven_backend.Data;
using lockhaven_backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using File = lockhaven_backend.Models.File;

namespace lockhaven_backend.Services;

public class FileService : IFileService
{
    private readonly ApplicationDbContext _dbContext;

    public FileService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<File> UploadFile(Stream fileStream, string fileName, string contentType, long fileSize, string userId)
    {
        // ADD FILE SIZE LIMIT CHECK?

        // var file = new File
        // {
        //     // ... other properties
        //     IsClientEncrypted = false, // Will be true for client-side encryption
        // };

        // if (!file.IsClientEncrypted)
        // {
        //     // Server-side encryption (current approach)
        //     var key = await GenerateEncryptionKey();
        //     var iv = GenerateInitializationVector();
        //     file.EncryptedKey = Convert.ToBase64String(key);
        //     file.InitializationVector = Convert.ToBase64String(iv);
            
        //     // Encrypt the file stream
        //     fileStream = await EncryptStream(fileStream, key, iv);
        // }
        // else
        // {
        //     // Client-side encryption (future approach)
        //     // File is already encrypted, just store it
        //     file.EncryptedKey = string.Empty;
        //     file.InitializationVector = string.Empty;
        // }

        // // Store in blob storage
        // await _blobService.UploadBlobAsync(file.BlobPath, fileStream);
        
        // return file;
    }

    public async Task<Stream> DownloadFile(string fileId, string userId)
    {
        throw new NotImplementedException();
    }

    public async Task<File?> GetFileById(string fileId, string userId)
    {
        if (string.IsNullOrEmpty(fileId) || string.IsNullOrEmpty(userId)) 
        {
            throw new ArgumentNullException("fileId and userId cannot be null or empty");
        }

        try 
        {
            var file = await _dbContext.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);

            return file;
        }
        catch (Exception ex) 
        {
            throw new Exception($"Error getting file by id: {fileId}", ex);
        }
    }

    public async Task<ICollection<File>> GetUserFiles(string userId)
    {
        if (string.IsNullOrEmpty(userId)) 
        {
            throw new ArgumentNullException("userId cannot be null or empty");
        }

        try 
        {
            var files = await _dbContext.Files.Where(f => f.UserId == userId).ToListAsync();

            return files;
        }
        catch (Exception ex) 
        {
            throw new Exception($"Error getting user files: {userId}", ex);
        }
    }

    // TODO: Delete file from blob storage
    public async Task<bool> DeleteFile(string fileId, string userId)
    {
        if (string.IsNullOrEmpty(fileId) || string.IsNullOrEmpty(userId)) 
        {
            throw new ArgumentNullException("fileId and userId cannot be null or empty");
        }

        try 
        {
            var file = await _dbContext.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);

            if (file == null) 
            {
                return false;
            }

            _dbContext.Files.Remove(file);
            await _dbContext.SaveChangesAsync();

            return true;
        }
        catch (Exception ex) 
        {
            throw new Exception($"Error deleting file: {fileId}", ex);
        }
    }

    public async Task<bool> UserOwnsFile(string fileId, string userId)
    {
        if (string.IsNullOrEmpty(fileId) || string.IsNullOrEmpty(userId)) 
        {
            throw new ArgumentNullException("fileId and userId cannot be null or empty");
        }

        try 
        {
            return await _dbContext.Files
                .AnyAsync(f => f.Id == fileId && f.UserId == userId);
        }
        catch (Exception ex) 
        {
            throw new Exception($"Error checking if user owns file: {fileId}", ex);
        }
    }

    public async Task<long> GetUserStorageUsed(string userId)
    {
        throw new NotImplementedException();
    }

    public bool IsFileTypeAllowed(string fileType)
    {
        if (string.IsNullOrEmpty(fileType))
        {
            return false;
        }

        return AcceptedFileTypes.AllowedFileTypes.Contains(fileType);
    }

    public byte[] GenerateEncryptionKey()
    {   
        return RandomNumberGenerator.GetBytes(32);
    }

    public byte[] GenerateInitializationVector()
    {
        return RandomNumberGenerator.GetBytes(12);
    }
}   