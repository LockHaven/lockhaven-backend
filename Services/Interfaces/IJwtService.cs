using System.Security.Claims;
using lockhaven_backend.Models;

namespace lockhaven_backend.Services.Interfaces;

public interface IJwtService
{
    string GenerateToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}