using System.Security.Claims;
using lockhaven_backend.Models;
using lockhaven_backend.Models.Requests;
using lockhaven_backend.Models.Responses;
using lockhaven_backend.Services.Interfaces;

namespace lockhaven_backend.Services;

public class AuthService : IAuthService
{
    private readonly IJwtService _jwtService;
    private readonly List<User> _users = new(); // TODO: Replace with database

    public AuthService(IJwtService jwtService)
    {
        _jwtService = jwtService;
    }

    public Task<AuthResponse> Register(RegisterRequest request)
    {
        if (_users.Any(u => u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
        {
            return Task.FromResult(new AuthResponse
            {
                Success = false,
                Message = "User with this email already exists"
            });
        }

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Role = Role.User,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _users.Add(user);

        var token = _jwtService.GenerateToken(user);

        return Task.FromResult(new AuthResponse
        {
            Success = true,
            Message = "User registered successfully",
            Token = token,
            User = new UserResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin
            }
        });
    }

    public Task<AuthResponse> Login(LoginRequest request)
    {
        var user = _users.FirstOrDefault(u => u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase));

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Task.FromResult(new AuthResponse { Success = false, Message = "Invalid credentials" });
        }

        user.LastLogin = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        var token = _jwtService.GenerateToken(user);

        return Task.FromResult(new AuthResponse 
        { 
            Success = true, 
            Message = "Login successful", 
            Token = token, 
            User = new UserResponse 
            { 
                Id = user.Id, 
                FirstName = user.FirstName, 
                LastName = user.LastName, 
                Email = user.Email, 
                Role = user.Role, 
                CreatedAt = user.CreatedAt, 
                LastLogin = user.LastLogin                 
            } 
        });
    }

    public Task<UserResponse> GetProfile(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        var userEntity = _users.FirstOrDefault(u => u.Id == userId);

        if (userEntity == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        return Task.FromResult(
            new UserResponse
            {
                Id = userEntity.Id,
                FirstName = userEntity.FirstName,
                LastName = userEntity.LastName,
                Email = userEntity.Email,
                Role = userEntity.Role,
                CreatedAt = userEntity.CreatedAt,
                LastLogin = userEntity.LastLogin
            }
        );
    }
}