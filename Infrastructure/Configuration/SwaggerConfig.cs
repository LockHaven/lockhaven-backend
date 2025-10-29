using lockhaven_backend.Filters;
using Microsoft.OpenApi.Models;

namespace lockhaven_backend.Infrastructure.Configuration;

/// <summary>
/// Provides Swagger/OpenAPI configuration for the LockHaven API.
/// </summary>
public static class SwaggerConfig
{
    /// <summary>
    /// Adds Swagger generation and configuration for the LockHaven API,
    /// including JWT Bearer authentication and file upload support.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add Swagger services to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddLockHavenSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "LockHaven API",
                Version = "v1",
                Description = "Secure file storage API with end-to-end encryption",
                Contact = new OpenApiContact
                {
                    Name = "LockHaven",
                    Url = new Uri("https://github.com/LockHaven/lockhaven-backend")
                }
            });

            // Add JWT Bearer support
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // File upload operation filter
            options.OperationFilter<FileUploadOperation>();
        });

        return services;
    }
}
