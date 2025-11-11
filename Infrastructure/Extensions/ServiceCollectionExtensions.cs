using System.Text;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
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
    /// Registers all LockHaven application services, including Key Vault, authentication,
    /// authorization, database, and blob storage connections.
    /// </summary>
    /// <param name="services">The DI container.</param>
    /// <param name="config">Application configuration.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddLockHavenServices(this IServiceCollection services, IConfiguration config)
    {
        // -------------------------------
        // Core ASP.NET setup
        // -------------------------------
        services.AddControllers();

        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.WithOrigins("http://localhost:3000") // Adjust if frontend moves to Azure
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        // -------------------------------
        // JWT Authentication
        // -------------------------------
        var jwtIssuer  = config["Jwt:Issuer"]  ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        var jwtAudience = config["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
        var jwtKey     = config["Jwt:Key"];

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // If KeyVault-backed key is being used, JwtService will handle it;
                // for token validation we still need a symmetric key here.
                var signingKey = jwtKey is { Length: > 0 }
                    ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    : throw new InvalidOperationException("Jwt:Key must be configured for token validation.");

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = signingKey
                };
            });

        services.AddAuthorization();

        // -------------------------------
        // Database (Azure SQL)
        // -------------------------------
        const string connectionName = "SqlServer";
        var connectionString = config.GetConnectionString(connectionName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection string not configured.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // -------------------------------
        // Azure Blob Storage
        // -------------------------------
        var blobConnectionString = config["BlobStorage:ConnectionString"]
            ?? config["BlobStorage__ConnectionString"] // support App Settings style
            ?? throw new InvalidOperationException("Blob storage connection string is not configured.");

        services.AddSingleton(_ => new BlobServiceClient(blobConnectionString));

        services.AddSingleton<IBlobStorageService, BlobStorageService>();

        // -------------------------------
        // Azure Key Vault & Key Encryption Service
        // -------------------------------
        var keyVaultUri = config["KeyVault:VaultUri"]
            ?? throw new InvalidOperationException("KeyVault:VaultUri is not configured");

        services.AddSingleton(_ => new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential()));
        services.AddSingleton<IKeyEncryptionService>(_ => new KeyEncryptionService(keyVaultUri));

        // -------------------------------
        // Business Services
        // -------------------------------
        services.AddSingleton<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IFileService, FileService>();

        // -------------------------------
        // Health Checks
        // -------------------------------
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("Database", failureStatus: HealthStatus.Unhealthy);

        return services;
    }
}
