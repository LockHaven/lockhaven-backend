using lockhaven_backend.Models.Requests;
using lockhaven_backend.Models.Responses;

namespace lockhaven_backend.Services.Interfaces;

public interface ISecretService
{
    Task<UpsertSecretResponse> UpsertSecretAsync(
        Guid projectId,
        Guid environmentId,
        string key,
        UpsertSecretRequest request,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<GetSecretResponse> GetSecretAsync(
        Guid projectId,
        Guid environmentId,
        string key,
        Guid userId,
        bool includeMetadata,
        int? version,
        CancellationToken cancellationToken = default);

    Task DeleteSecretAsync(
        Guid projectId,
        Guid environmentId,
        string key,
        Guid userId,
        CancellationToken cancellationToken = default);
}
