using System.Security.Claims;
using lockhaven_backend.Services.Interfaces;

namespace lockhaven_backend.Services.Implementations;

public sealed class CurrentUserService : ICurrentUserService
{
    public Guid UserId { get; }

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        var idValue = user.FindFirst(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(idValue?.Value))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        if (!Guid.TryParse(idValue.Value, out var userId))
        {
            throw new UnauthorizedAccessException("User ID is not a valid GUID");
        }

        UserId = userId;
    }
}