using lockhaven_backend.Data;
using lockhaven_backend.Models;
using lockhaven_backend.Models.Requests;
using lockhaven_backend.Models.Responses;
using lockhaven_backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace lockhaven_backend.Services;

public class ProjectService : IProjectService
{
    private readonly ApplicationDbContext _dbContext;

    public ProjectService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ProjectResponse> CreateProject(CreateProjectRequest request, Guid userId)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        var name = request.Name?.Trim();
        var slug = request.Slug?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name is required", nameof(request.Name));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Project slug is required", nameof(request.Slug));
        }

        if (await _dbContext.Projects.AnyAsync(p => p.OwnerUserId == userId && p.Slug == slug && !p.IsDeleted))
        {
            throw new InvalidOperationException("A project with this slug already exists for this owner");
        }

        var now = DateTime.UtcNow;
        var project = new Project
        {
            OwnerUserId = userId,
            Name = name,
            Slug = slug,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            IsDeleted = false
        };

        _dbContext.Projects.Add(project);
        _dbContext.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = project.Id,
            UserId = userId,
            Role = ProjectMemberRole.Owner,
            CreatedAtUtc = now
        });

        await _dbContext.SaveChangesAsync();
        return ToResponse(project, ProjectMemberRole.Owner);
    }

    public async Task<ICollection<ProjectResponse>> GetProjects(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        var memberships = await _dbContext.ProjectMembers
            .Where(pm => pm.UserId == userId && !pm.Project!.IsDeleted)
            .Select(pm => new { Project = pm.Project!, pm.Role })
            .ToListAsync();

        var ownedProjectsWithoutMembership = await _dbContext.Projects
            .Where(p => p.OwnerUserId == userId && !p.IsDeleted)
            .Where(p => !_dbContext.ProjectMembers.Any(pm => pm.ProjectId == p.Id && pm.UserId == userId))
            .ToListAsync();

        var results = memberships
            .Select(x => ToResponse(x.Project, x.Role))
            .ToDictionary(x => x.Id, x => x);

        foreach (var owned in ownedProjectsWithoutMembership)
        {
            results[owned.Id] = ToResponse(owned, ProjectMemberRole.Owner);
        }

        return results.Values
            .OrderBy(p => p.Name)
            .ToList();
    }

    public async Task<ProjectResponse> GetProjectById(Guid projectId, Guid userId)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project ID is required", nameof(projectId));
        }

        var (project, role) = await GetAuthorizedProject(projectId, userId);
        return ToResponse(project, role);
    }

    public async Task<ProjectResponse> UpdateProject(Guid projectId, UpdateProjectRequest request, Guid userId)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var (project, role) = await GetAuthorizedProject(projectId, userId);
        if (role != ProjectMemberRole.Owner && role != ProjectMemberRole.Admin)
        {
            throw new UnauthorizedAccessException("Only project owners or admins can update the project");
        }

        var newName = request.Name?.Trim();
        var newSlug = request.Slug?.Trim().ToLowerInvariant();
        var hasNameUpdate = !string.IsNullOrWhiteSpace(newName);
        var hasSlugUpdate = !string.IsNullOrWhiteSpace(newSlug);

        if (!hasNameUpdate && !hasSlugUpdate)
        {
            throw new BadHttpRequestException("At least one field must be provided to update the project");
        }

        var hasActualNameChange = hasNameUpdate && !string.Equals(project.Name, newName, StringComparison.Ordinal);
        var hasActualSlugChange = hasSlugUpdate && !string.Equals(project.Slug, newSlug, StringComparison.Ordinal);

        if (!hasActualNameChange && !hasActualSlugChange)
        {
            throw new BadHttpRequestException("No changes detected for project update");
        }

        if (hasActualNameChange)
        {
            project.Name = newName!;
        }

        if (hasActualSlugChange)
        {
            var slugTaken = await _dbContext.Projects.AnyAsync(p =>
                p.OwnerUserId == project.OwnerUserId &&
                p.Slug == newSlug! &&
                p.Id != project.Id &&
                !p.IsDeleted);

            if (slugTaken)
            {
                throw new InvalidOperationException("A project with this slug already exists for this owner");
            }

            project.Slug = newSlug!;
        }

        project.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
        return ToResponse(project, role);
    }

    public async Task<bool> DeleteProject(Guid projectId, Guid userId)
    {
        var (project, role) = await GetAuthorizedProject(projectId, userId);
        if (role != ProjectMemberRole.Owner)
        {
            throw new UnauthorizedAccessException("Only project owners can delete projects");
        }

        if (project.IsDeleted)
        {
            return true;
        }

        project.IsDeleted = true;
        project.DeletedAtUtc = DateTime.UtcNow;
        project.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    private async Task<(Project project, ProjectMemberRole role)> GetAuthorizedProject(Guid projectId, Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted)
            ?? throw new FileNotFoundException($"Project with id {projectId} not found");

        if (project.OwnerUserId == userId)
        {
            return (project, ProjectMemberRole.Owner);
        }

        var membership = await _dbContext.ProjectMembers
            .FirstOrDefaultAsync(pm => pm.ProjectId == projectId && pm.UserId == userId)
            ?? throw new UnauthorizedAccessException("You do not have access to this project");

        return (project, membership.Role);
    }

    private static ProjectResponse ToResponse(Project project, ProjectMemberRole role) => new()
    {
        Id = project.Id,
        OwnerUserId = project.OwnerUserId,
        Name = project.Name,
        Slug = project.Slug,
        MyRole = role,
        CreatedAtUtc = project.CreatedAtUtc,
        UpdatedAtUtc = project.UpdatedAtUtc
    };
}
