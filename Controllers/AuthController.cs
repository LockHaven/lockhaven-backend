using lockhaven_backend.Models.Requests;
using lockhaven_backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace lockhaven_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (request == null) 
        {
            throw new ArgumentNullException(nameof(request));
        }

        var result = await _authService.Register(request);
        return Ok(result);

    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (request == null) {
            throw new ArgumentNullException(nameof(request));
        }

        var result = await _authService.Login(request);
        return Ok(result);
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _authService.GetProfile(User);
        return Ok(result);
    }
}