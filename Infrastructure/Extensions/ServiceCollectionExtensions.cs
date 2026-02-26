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
    /// Registers all LockHaven application services, including key management, authentication,
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
                // Token validation uses a symmetric key from configuration.
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
        // Database (PostgreSQL)
        // -------------------------------
        const string connectionName = "Postgres";
        var connectionString = config.GetConnectionString(connectionName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("PostgreSQL connection string not configured.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // -------------------------------
        // Blob Storage
        // -------------------------------
        var provider = config["Storage:Provider"];

        if (string.Equals(provider, "Local", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IBlobStorageService, LocalFileStorageService>();
        }
        else
        {
            var blobConnectionString = config["BlobStorage:ConnectionString"]
                ?? config["BlobStorage__ConnectionString"]
                ?? throw new InvalidOperationException("Blob storage connection string is not configured.");

            services.AddSingleton(_ => new BlobServiceClient(blobConnectionString));
            services.AddSingleton<IBlobStorageService, BlobStorageService>();
        }

        // -------------------------------
        // Key Encryption
        // -------------------------------
        var keyProvider = config["KeyEncryption:Provider"] ?? "VaultTransit";

        if (string.Equals(keyProvider, "VaultTransit", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient("VaultTransit");
            services.AddSingleton<IKeyEncryptionService>(serviceProvider =>
            {
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var httpClient = httpClientFactory.CreateClient("VaultTransit");
                return new VaultTransitKeyEncryptionService(httpClient, config);
            });
        }
        else
        {
            throw new InvalidOperationException($"Unsupported key encryption provider '{keyProvider}'.");
        }

        // -------------------------------
        // Business Services
        // -------------------------------
        services.AddSingleton<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IFileValidationService, FileValidationService>();

        // -------------------------------
        // Health Checks
        // -------------------------------
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("Database", failureStatus: HealthStatus.Unhealthy);

        return services;
    }
}
