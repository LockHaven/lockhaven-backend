using System.Text;
using Azure.Storage.Blobs;
using lockhaven_backend.Data;
using lockhaven_backend.Services;
using lockhaven_backend.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

namespace lockhaven_backend.Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for registering application services,
/// dependencies, and configuration settings for the LockHaven API.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all LockHaven application services, including authentication,
    /// authorization, database context, and storage services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="config">The application configuration.</param>
    /// <param name="env">The hosting environment.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddLockHavenServices(this IServiceCollection services, IConfiguration config, IWebHostEnvironment env)
    {
        // MVC Controllers
        services.AddControllers();

        // CORS configuration
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins("http://localhost:3000")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        // JWT authentication setup
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(config["Jwt:Key"]
                            ?? throw new InvalidOperationException("JWT Key is not configured"))
                    )
                };
            });

        services.AddAuthorization();

        // Database (SQLite for local dev)
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(config.GetConnectionString("DefaultConnection")));

        // Blob storage (switches automatically based on environment)
        services.AddSingleton(sp =>
        {
            var connectionString = config["BlobStorage:ConnectionString"]
                ?? throw new InvalidOperationException("BlobStorage:ConnectionString is not configured");
            return new BlobServiceClient(connectionString);
        });

        if (env.IsDevelopment())
        {
            services.AddScoped<IBlobStorageService, LocalFileStorageService>();
        }
        else
        {
            services.AddSingleton<IBlobStorageService, BlobStorageService>();
        }

        // Core business services
        services.AddSingleton<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IFileService, FileService>();

        // Health Checks
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("Database", failureStatus: HealthStatus.Unhealthy);

        return services;
    }
}
