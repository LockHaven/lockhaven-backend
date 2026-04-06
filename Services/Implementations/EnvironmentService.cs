using lockhaven_backend.Constants;
using lockhaven_backend.Data;
using lockhaven_backend.Models;
using lockhaven_backend.Models.Requests;
using lockhaven_backend.Models.Responses;
using lockhaven_backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace lockhaven_backend.Services;

public class EnvironmentService : IEnvironmentService
{
    private readonly ApplicationDbContext _dbContext;

    public EnvironmentService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EnvironmentResponse> CreateEnvironment(Guid projectId, CreateEnvironmentRequest request, Guid userId)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var (_, role) = await GetAuthorizedProject(projectId, userId);
        if (role != ProjectMemberRole.Owner && role != ProjectMemberRole.Admin)
        {
            throw new UnauthorizedAccessException("Only project owners or admins can create environments");
        }

        var name = request.Name?.Trim();
        var slug = SlugConstraints.NormalizeSlug(request.Slug);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Environment name is required", nameof(request.Name));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Environment slug is required", nameof(request.Slug));
        }

        if (!SlugConstraints.IsValid(slug))
        {
            throw new BadHttpRequestException(
                $"Environment slug must contain only {SlugConstraints.PatternDescription}.");
        }

        if (await _dbContext.Environments.AnyAsync(e =>
                e.ProjectId == projectId && e.Slug == slug && !e.IsDeleted))
        {
            throw new InvalidOperationException("An environment with this slug already exists for this project");
        }

        var now = DateTime.UtcNow;
        var environment = new ProjectEnvironment
        {
            ProjectId = projectId,
            Name = name,
            Slug = slug,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.Environments.Add(environment);
        await _dbContext.SaveChangesAsync();

        return ToResponse(environment);
    }

    public async Task<ICollection<EnvironmentResponse>> GetEnvironments(Guid projectId, Guid userId)
    {
        await GetAuthorizedProject(projectId, userId);

        var environments = await _dbContext.Environments
            .Where(e => e.ProjectId == projectId && !e.IsDeleted)
            .OrderBy(e => e.Name)
            .ToListAsync();

        return environments.Select(ToResponse).ToList();
    }

    public async Task<EnvironmentResponse> UpdateEnvironment(Guid projectId, Guid environmentId, UpdateEnvironmentRequest request, Guid userId)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var (_, role) = await GetAuthorizedProject(projectId, userId);
        if (role != ProjectMemberRole.Owner && role != ProjectMemberRole.Admin)
        {
            throw new UnauthorizedAccessException("Only project owners or admins can update environments");
        }

        var environment = await _dbContext.Environments
            .FirstOrDefaultAsync(e => e.Id == environmentId && e.ProjectId == projectId && !e.IsDeleted)
            ?? throw new FileNotFoundException($"Environment with id {environmentId} not found");

        var newName = request.Name?.Trim();
        var newSlug = SlugConstraints.NormalizeSlug(request.Slug);
        var hasNameUpdate = !string.IsNullOrWhiteSpace(newName);
        var hasSlugUpdate = !string.IsNullOrWhiteSpace(newSlug);

        if (!hasNameUpdate && !hasSlugUpdate)
        {
            throw new BadHttpRequestException("At least one field must be provided to update the environment");
        }

        var hasActualNameChange = hasNameUpdate && !string.Equals(environment.Name, newName, StringComparison.Ordinal);
        var hasActualSlugChange = hasSlugUpdate && !string.Equals(environment.Slug, newSlug, StringComparison.Ordinal);

        if (!hasActualNameChange && !hasActualSlugChange)
        {
            throw new BadHttpRequestException("No changes detected for environment update");
        }

        if (hasActualNameChange)
        {
            environment.Name = newName!;
        }

        if (hasActualSlugChange)
        {
            if (!SlugConstraints.IsValid(newSlug!))
            {
                throw new BadHttpRequestException(
                    $"Environment slug must contain only {SlugConstraints.PatternDescription}.");
            }

            var slugTaken = await _dbContext.Environments.AnyAsync(e =>
                e.ProjectId == projectId &&
                e.Slug == newSlug! &&
                e.Id != environmentId &&
                !e.IsDeleted);

            if (slugTaken)
            {
                throw new InvalidOperationException("An environment with this slug already exists for this project");
            }

            environment.Slug = newSlug!;
        }

        environment.UpdatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return ToResponse(environment);
    }

    public async Task<bool> DeleteEnvironment(Guid projectId, Guid environmentId, Guid userId)
    {
        var (_, role) = await GetAuthorizedProject(projectId, userId);
        if (role != ProjectMemberRole.Owner && role != ProjectMemberRole.Admin)
        {
            throw new UnauthorizedAccessException("Only project owners or admins can delete environments");
        }

        var environment = await _dbContext.Environments
            .FirstOrDefaultAsync(e => e.Id == environmentId && e.ProjectId == projectId && !e.IsDeleted)
            ?? throw new FileNotFoundException($"Environment with id {environmentId} not found");

        var now = DateTime.UtcNow;

        var secrets = await _dbContext.Secrets
            .Where(s => s.EnvironmentId == environmentId && !s.IsDeleted)
            .ToListAsync();

        foreach (var secret in secrets)
        {
            secret.IsDeleted = true;
            secret.DeletedAtUtc = now;
            secret.UpdatedAtUtc = now;
        }

        environment.IsDeleted = true;
        environment.DeletedAtUtc = now;
        environment.UpdatedAtUtc = now;

        await _dbContext.SaveChangesAsync();
        return true;
    }

    private async Task<(Project project, ProjectMemberRole role)> GetAuthorizedProject(Guid projectId, Guid userId)
    {
        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Project ID is required", nameof(projectId));
        }

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

    private static EnvironmentResponse ToResponse(ProjectEnvironment environment) => new()
    {
        Id = environment.Id,
        ProjectId = environment.ProjectId,
        Name = environment.Name,
        Slug = environment.Slug,
        CreatedAtUtc = environment.CreatedAtUtc,
        UpdatedAtUtc = environment.UpdatedAtUtc
    };
}
