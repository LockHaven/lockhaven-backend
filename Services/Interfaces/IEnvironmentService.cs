using lockhaven_backend.Models.Requests;
using lockhaven_backend.Models.Responses;

namespace lockhaven_backend.Services.Interfaces;

public interface IEnvironmentService
{
    Task<EnvironmentResponse> CreateEnvironment(Guid projectId, CreateEnvironmentRequest request, Guid userId);
    Task<ICollection<EnvironmentResponse>> GetEnvironments(Guid projectId, Guid userId);
    Task<EnvironmentResponse> UpdateEnvironment(Guid projectId, Guid environmentId, UpdateEnvironmentRequest request, Guid userId);
    Task<bool> DeleteEnvironment(Guid projectId, Guid environmentId, Guid userId);
}
