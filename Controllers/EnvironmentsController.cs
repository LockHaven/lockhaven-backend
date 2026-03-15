using lockhaven_backend.Models.Requests;
using lockhaven_backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace lockhaven_backend.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/environments")]
[Authorize]
public class EnvironmentsController : ControllerBase
{
    private readonly IEnvironmentService _environmentService;
    private readonly ICurrentUserService _currentUserService;

    public EnvironmentsController(IEnvironmentService environmentService, ICurrentUserService currentUserService)
    {
        _environmentService = environmentService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateEnvironment(Guid projectId, CreateEnvironmentRequest request)
    {
        if (projectId == Guid.Empty)
        {
            throw new BadHttpRequestException("Project ID is required");
        }

        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var result = await _environmentService.CreateEnvironment(projectId, request, _currentUserService.UserId);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetEnvironments(Guid projectId)
    {
        if (projectId == Guid.Empty)
        {
            throw new BadHttpRequestException("Project ID is required");
        }

        var result = await _environmentService.GetEnvironments(projectId, _currentUserService.UserId);
        return Ok(result);
    }

    [HttpPatch("{envId:guid}")]
    public async Task<IActionResult> UpdateEnvironment(Guid projectId, Guid envId, UpdateEnvironmentRequest request)
    {
        if (projectId == Guid.Empty)
        {
            throw new BadHttpRequestException("Project ID is required");
        }

        if (envId == Guid.Empty)
        {
            throw new BadHttpRequestException("Environment ID is required");
        }

        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var result = await _environmentService.UpdateEnvironment(projectId, envId, request, _currentUserService.UserId);
        return Ok(result);
    }

    [HttpDelete("{envId:guid}")]
    public async Task<IActionResult> DeleteEnvironment(Guid projectId, Guid envId)
    {
        if (projectId == Guid.Empty)
        {
            throw new BadHttpRequestException("Project ID is required");
        }

        if (envId == Guid.Empty)
        {
            throw new BadHttpRequestException("Environment ID is required");
        }

        await _environmentService.DeleteEnvironment(projectId, envId, _currentUserService.UserId);
        return Ok(new { message = "Environment deleted successfully" });
    }
}
