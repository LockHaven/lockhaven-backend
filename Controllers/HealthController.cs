using lockhaven_backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace lockhaven_backend.Controllers;

/// <summary>
/// Provides an API endpoint to check the health and readiness of the LockHaven backend.
/// Useful for uptime monitoring, load balancers, and deployment readiness checks.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public HealthController(ApplicationDbContext dbContext, IWebHostEnvironment env, IConfiguration config)
    {
        _dbContext = dbContext;
        _env = env;
        _config = config;
    }

    /// <summary>
    /// Returns the health status of the API, including application version,
    /// environment, and database connectivity.
    /// </summary>
    /// <returns>Health status as JSON.</returns>
    [HttpGet]
    [Route("")]
    public async Task<IActionResult> GetHealth()
    {
        var health = new
        {
            app = "LockHaven API",
            version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown",
            environment = _env.EnvironmentName,
            timestampUtc = DateTime.UtcNow,
            database = await CheckDatabaseAsync(),
            status = "Healthy"
        };

        return Ok(health);
    }

    /// <summary>
    /// Performs a lightweight database connectivity check.
    /// </summary>
    private async Task<object> CheckDatabaseAsync()
    {
        try
        {
            // Simple "heartbeat" query (no tracked entities)
            var canConnect = await _dbContext.Database.CanConnectAsync();
            return new
            {
                reachable = canConnect,
                provider = _dbContext.Database.ProviderName,
                connectionString = _config.GetConnectionString("DefaultConnection") != null ? "Configured" : "Missing"
            };
        }
        catch (Exception ex)
        {
            return new
            {
                reachable = false,
                error = ex.Message
            };
        }
    }
}
