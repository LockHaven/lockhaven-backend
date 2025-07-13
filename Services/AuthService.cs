using System.Security.Claims;
using lockhaven_backend.Data;
using lockhaven_backend.Models;
using lockhaven_backend.Models.Requests;
using lockhaven_backend.Models.Responses;
using lockhaven_backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace lockhaven_backend.Services;

public class AuthService : IAuthService
{
    private readonly IJwtService _jwtService;
    private readonly ApplicationDbContext _dbContext;
    private readonly List<User> _users = new(); // TODO: Replace with database

    public AuthService(IJwtService jwtService, ApplicationDbContext dbContext)
    {
        _jwtService = jwtService;
        _dbContext = dbContext;
    }

    public async Task<AuthResponse> Register(RegisterRequest request)
    {
        if (await _dbContext.Users.AnyAsync(u => u.Email.ToLower() == request.Email.ToLower()))
        {
            return new AuthResponse
            {
                Success = false,
                Message = "User with this email already exists"
            };
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

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        return new AuthResponse
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
        };
    }

    public async Task<AuthResponse> Login(LoginRequest request)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return new AuthResponse { Success = false, Message = "Invalid credentials" };
        }

        user.LastLogin = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);

        return new AuthResponse 
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
        };
    }

    public async Task<UserResponse> GetProfile(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        var userEntity = await _dbContext.Users.FindAsync(userId);

        if (userEntity == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        return new UserResponse
        {
            Id = userEntity.Id,
            FirstName = userEntity.FirstName,
            LastName = userEntity.LastName,
            Email = userEntity.Email,
            Role = userEntity.Role,
            CreatedAt = userEntity.CreatedAt,
            LastLogin = userEntity.LastLogin
        };
    }
}