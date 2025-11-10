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
        // Azure Key Vault
        // -------------------------------
        var vaultUri = config["KeyVault:VaultUri"]
            ?? throw new InvalidOperationException("KeyVault:VaultUri is not configured.");

        var secretClient = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
        services.AddSingleton(secretClient);

        // -------------------------------
        // JWT Authentication
        // -------------------------------
        var jwtKey = config["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey))
        {
            // Attempt to load from Key Vault if not provided
            var secretName = config["Jwt:KeyVaultSecretName"]
                ?? throw new InvalidOperationException("Jwt:KeyVaultSecretName is not configured.");
            var keyVaultSecret = secretClient.GetSecret(secretName);
            jwtKey = keyVaultSecret.Value.Value;
        }

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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };
            });

        services.AddAuthorization();

        // -------------------------------
        // Database
        // -------------------------------
        var connectionString = config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Database connection string not configured.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (connectionString.Contains("Data Source=")) // SQLite fallback
                options.UseSqlite(connectionString);
            else
                options.UseSqlServer(connectionString);
        });

        // -------------------------------
        // Azure Blob Storage
        // -------------------------------
        var blobConnection = config["BlobStorage:ConnectionString"]
            ?? throw new InvalidOperationException("BlobStorage:ConnectionString is not configured.");

        services.AddSingleton(_ => new BlobServiceClient(blobConnection));
        services.AddSingleton<IBlobStorageService, BlobStorageService>();

        // -------------------------------
        // Business Services
        // -------------------------------
        services.AddSingleton<IJwtService, JwtService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IFileService, FileService>();
        services.AddSingleton<IKeyEncryptionService>(sp => new KeyEncryptionService(vaultUri));

        // -------------------------------
        // Health Checks
        // -------------------------------
        services.AddHealthChecks()
            .AddDbContextCheck<ApplicationDbContext>("Database", failureStatus: HealthStatus.Unhealthy);

        return services;
    }
}
