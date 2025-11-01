using lockhaven_backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        => User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? throw new UnauthorizedAccessException("User ID not found in token");

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new BadHttpRequestException("File is null or empty");

        var userId = GetUserId();
        var result = await _fileService.UploadFile(
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            file.Length,
            userId);

        return Ok(new
        {
            message = "File uploaded successfully",
            fileId = result.Id,
            fileName = result.Name,
            size = result.Size
        });
    }

    [HttpGet("download/{fileId}")]
    public async Task<IActionResult> DownloadFile(string fileId)
    {
        if (string.IsNullOrEmpty(fileId))
            throw new BadHttpRequestException("File ID is required");

        var userId = GetUserId();
        var fileMetadata = await _fileService.GetFileById(fileId, userId);
        var fileStream = await _fileService.DownloadFile(fileId, userId);

        return File(fileStream, fileMetadata.ContentType, fileMetadata.Name);
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetUserFiles()
    {
        var userId = GetUserId();
        var files = await _fileService.GetUserFiles(userId);
        return Ok(files);
    }

    [HttpDelete("{fileId}")]
    public async Task<IActionResult> DeleteFile(string fileId)
    {
        if (string.IsNullOrEmpty(fileId))
            throw new BadHttpRequestException("File ID is required");

        var userId = GetUserId();
        await _fileService.DeleteFile(fileId, userId);

        return Ok(new { message = "File deleted successfully" });
    }

    [HttpGet("storage")]
    public async Task<IActionResult> GetStorageUsed()
    {
        var userId = GetUserId();
        var storageUsed = await _fileService.GetUserStorageUsed(userId);

        return Ok(new
        {
            storageUsedBytes = storageUsed,
            storageUsedMB = Math.Round(storageUsed / (1024.0 * 1024.0), 2)
        });
    }
}
