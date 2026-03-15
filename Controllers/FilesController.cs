using lockhaven_backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace lockhaven_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly ICurrentUserService _currentUserService;

    public FilesController(IFileService fileService, ICurrentUserService currentUserService)
    {
        _fileService = fileService;
        _currentUserService = currentUserService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null)
            throw new BadHttpRequestException("File is null or empty");

        var result = await _fileService.UploadFile(file, _currentUserService.UserId);

        return Ok(new
        {
            message = "File uploaded successfully",
            fileId = result.Id,
            fileName = result.Name,
            size = result.Size
        });
    }

    [HttpGet("download/{fileId:guid}")]
    public async Task<IActionResult> DownloadFile(Guid fileId)
    {
        if (fileId == Guid.Empty)
            throw new BadHttpRequestException("File ID is required");

        var fileMetadata = await _fileService.GetFileById(fileId, _currentUserService.UserId)
            ?? throw new FileNotFoundException($"File with id {fileId} not found");
        var fileStream = await _fileService.DownloadFile(fileId, _currentUserService.UserId);

        return File(fileStream, fileMetadata.ContentType, fileMetadata.Name);
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetUserFiles()
    {
        var files = await _fileService.GetUserFiles(_currentUserService.UserId);
        return Ok(files);
    }

    [HttpDelete("{fileId:guid}")]
    public async Task<IActionResult> DeleteFile(Guid fileId)
    {
        if (fileId == Guid.Empty)
            throw new BadHttpRequestException("File ID is required");

        await _fileService.DeleteFile(fileId, _currentUserService.UserId);

        return Ok(new { message = "File deleted successfully" });
    }

    [HttpGet("storage")]
    public async Task<IActionResult> GetStorageUsed()
    {
        var storageUsed = await _fileService.GetUserStorageUsed(_currentUserService.UserId);

        return Ok(new
        {
            storageUsedBytes = storageUsed,
            storageUsedMB = Math.Round(storageUsed / (1024.0 * 1024.0), 2)
        });
    }
}
