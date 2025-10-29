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

    public AuthService(ApplicationDbContext dbContext, IJwtService jwtService)
    {
        _dbContext = dbContext;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse> Register(RegisterRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Email is required", nameof(request.Email));
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Password is required", nameof(request.Password));
        }

        var email = request.Email.Trim().ToLower();

        if (await _dbContext.Users.AnyAsync(u => u.Email == email))
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Role = Role.User,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password.Trim()),
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
            User = ToUserResponse(user)
        };
    }

    public async Task<AuthResponse> Login(LoginRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var email = request.Email?.Trim().ToLower()
            ?? throw new ArgumentException("Email is required");

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password.Trim(), user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid credentials");
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
            User = ToUserResponse(user)
        };
    }

    public async Task<UserResponse> GetProfile(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("User not authenticated");

        var userEntity = await _dbContext.Users.FindAsync(userId)
            ?? throw new UnauthorizedAccessException("User not found");

        return ToUserResponse(userEntity);
    }

    private static UserResponse ToUserResponse(User user) => new()
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = user.Role,
        CreatedAt = user.CreatedAt,
        LastLogin = user.LastLogin
    };
}