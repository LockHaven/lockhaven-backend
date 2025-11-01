using System.Security.Claims;
using lockhaven_backend.Models.Requests;
using lockhaven_backend.Models.Responses;

namespace lockhaven_backend.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> Register(RegisterRequest request);
    Task<AuthResponse> Login(LoginRequest request);
    Task<UserResponse> GetProfile(ClaimsPrincipal user);
}