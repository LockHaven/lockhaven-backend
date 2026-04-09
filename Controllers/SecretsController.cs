using lockhaven_backend.Models.Requests;
using lockhaven_backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace lockhaven_backend.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/environments/{envId:guid}/secrets")]
[Authorize]
public class SecretsController : ControllerBase
{
    private readonly ISecretService _secretService;
    private readonly ICurrentUserService _currentUserService;

    public SecretsController(ISecretService secretService, ICurrentUserService currentUserService)
    {
        _secretService = secretService;
        _currentUserService = currentUserService;
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> UpsertSecret(
        Guid projectId,
        Guid envId,
        string key,
        [FromBody] UpsertSecretRequest request)
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

        var result = await _secretService.UpsertSecretAsync(
            projectId,
            envId,
            key,
            request,
            _currentUserService.UserId);

        return Ok(result);
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> GetSecret(
        Guid projectId,
        Guid envId,
        string key,
        [FromQuery] bool includeMetadata = false,
        [FromQuery] int? version = null)
    {
        if (projectId == Guid.Empty)
        {
            throw new BadHttpRequestException("Project ID is required");
        }

        if (envId == Guid.Empty)
        {
            throw new BadHttpRequestException("Environment ID is required");
        }

        var result = await _secretService.GetSecretAsync(
            projectId,
            envId,
            key,
            _currentUserService.UserId,
            includeMetadata,
            version);

        return Ok(result);
    }

    [HttpDelete("{key}")]
    public async Task<IActionResult> DeleteSecret(Guid projectId, Guid envId, string key)
    {
        if (projectId == Guid.Empty)
        {
            throw new BadHttpRequestException("Project ID is required");
        }

        if (envId == Guid.Empty)
        {
            throw new BadHttpRequestException("Environment ID is required");
        }

        await _secretService.DeleteSecretAsync(projectId, envId, key, _currentUserService.UserId);
        return NoContent();
    }
}
