using lockhaven_backend.Models.Requests;
using lockhaven_backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace lockhaven_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ICurrentUserService _currentUserService;

    public ProjectsController(IProjectService projectService, ICurrentUserService currentUserService)
    {
        _projectService = projectService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject(CreateProjectRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var result = await _projectService.CreateProject(request, _currentUserService.UserId);
        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetProjects()
    {
        var result = await _projectService.GetProjects(_currentUserService.UserId);
        return Ok(result);
    }

    [HttpGet("{projectId:guid}")]
    public async Task<IActionResult> GetProject(Guid projectId)
    {
        if (projectId == Guid.Empty)
        {
            throw new BadHttpRequestException("Project ID is required");
        }

        var result = await _projectService.GetProjectById(projectId, _currentUserService.UserId);
        return Ok(result);
    }

    [HttpPatch("{projectId:guid}")]
    public async Task<IActionResult> UpdateProject(Guid projectId, UpdateProjectRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (projectId == Guid.Empty)
        {
            throw new BadHttpRequestException("Project ID is required");
        }

        var result = await _projectService.UpdateProject(projectId, request, _currentUserService.UserId);
        return Ok(result);
    }

    [HttpDelete("{projectId:guid}")]
    public async Task<IActionResult> DeleteProject(Guid projectId)
    {
        if (projectId == Guid.Empty)
        {
            throw new BadHttpRequestException("Project ID is required");
        }

        await _projectService.DeleteProject(projectId, _currentUserService.UserId);
        return Ok(new { message = "Project deleted successfully" });
    }
}