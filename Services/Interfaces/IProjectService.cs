using lockhaven_backend.Models.Requests;
using lockhaven_backend.Models.Responses;

namespace lockhaven_backend.Services.Interfaces;

public interface IProjectService
{
    Task<ProjectResponse> CreateProject(CreateProjectRequest request, Guid userId);
    Task<ICollection<ProjectResponse>> GetProjects(Guid userId);
    Task<ProjectResponse> GetProjectById(Guid projectId, Guid userId);
    Task<ProjectResponse> UpdateProject(Guid projectId, UpdateProjectRequest request, Guid userId);
    Task<bool> DeleteProject(Guid projectId, Guid userId);
}
