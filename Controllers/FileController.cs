using lockhaven_backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;

namespace lockhaven_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FileController : ControllerBase
{
    private readonly IFileService _fileService;

    public FileController(IFileService fileService)
    {
        _fileService = fileService;
    }

    private string GetUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? throw new UnauthorizedAccessException("User ID not found in token");
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest();
            }

            // Validate file type
            if (!_fileService.IsFileTypeAllowed(Path.GetExtension(file.FileName)))
            {
                return BadRequest();
            }

            // TODO: Add file size limit validation
            // if (file.Length > MaxFileSize) return BadRequest("File too large");

            var userId = GetUserId();
            var result = await _fileService.UploadFile(
                file.OpenReadStream(), 
                file.FileName, 
                file.ContentType, 
                file.Length, 
                userId);

            return Ok(new { 
                message = "File uploaded successfully", 
                fileId = result.Id,
                fileName = result.Name,
                size = result.Size
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized();
        }
        catch (Exception)
        {
            return Problem("Error uploading file");
        }
    }

    [HttpGet("download/{fileId}")]
    public async Task<IActionResult> DownloadFile(string fileId)
    {
        try
        {
            if (string.IsNullOrEmpty(fileId))
            {
                return BadRequest("File ID is required");
            }

            var userId = GetUserId();
            var fileMetadata = await _fileService.GetFileById(fileId, userId);

            var fileStream = await _fileService.DownloadFile(fileId, userId);
            return File(fileStream, fileMetadata.ContentType, fileMetadata.Name);
        }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (ArgumentException) { return BadRequest(); }
        catch (FileNotFoundException) { return NotFound(); }
        catch (IOException) { return StatusCode(503); }
        catch (FormatException) { return StatusCode(500); }
        catch (CryptographicException) { return StatusCode(500); }
        catch (Exception) { return Problem("An unexpected error occurred."); }
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetUserFiles()
    {
        try
        {
            var userId = GetUserId();
            var files = await _fileService.GetUserFiles(userId);
            
            return Ok(files);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized();
        }
        catch (Exception ex)
        {
            return Problem("Error retrieving files");
        }
    }

    [HttpDelete("{fileId}")]
    public async Task<IActionResult> DeleteFile(string fileId)
    {
        try
        {
            if (string.IsNullOrEmpty(fileId))
            {
                return BadRequest("File ID is required");
            }

            var userId = GetUserId();
            var success = await _fileService.DeleteFile(fileId, userId);

            return Ok(new { message = "File deleted successfully" });
        }
        catch (UnauthorizedAccessException) { return Unauthorized(); }
        catch (ArgumentException) { return BadRequest(); }
        catch (FileNotFoundException) { return NotFound(); }
        catch (IOException) { return StatusCode(503); }
        catch (DbUpdateConcurrencyException) { return Conflict("A concurrency conflict occurred."); }
        catch (DbUpdateException) { return StatusCode(500); }
        catch (Exception) { return Problem("An unexpected error occurred."); }
    }

    [HttpGet("storage")]
    public async Task<IActionResult> GetStorageUsed()
    {
        try
        {
            var userId = GetUserId();
            var storageUsed = await _fileService.GetUserStorageUsed(userId);
            
            return Ok(new { 
                storageUsedBytes = storageUsed,
                storageUsedMB = Math.Round(storageUsed / (1024.0 * 1024.0), 2)
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (Exception)
        {
            return Problem("Error retrieving storage info");
        }
    }
}