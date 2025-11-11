using lockhaven_backend.Infrastructure.Health;
using lockhaven_backend.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace lockhaven_backend.Infrastructure.Extensions;

/// <summary>
/// Provides extension methods for configuring the LockHaven HTTP request pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Configures global middleware used by the LockHaven API application,
    /// including exception handling, authentication, and CORS.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> instance.</param>
    /// <returns>The same <see cref="IApplicationBuilder"/> instance for chaining.</returns>
    public static IApplicationBuilder UseLockHavenMiddleware(this IApplicationBuilder app)
    {
        var config = app.ApplicationServices.GetRequiredService<IConfiguration>();

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseCors("AllowFrontend");
        app.UseAuthentication();
        app.UseAuthorization();

        if (config.GetValue<bool>("Features:EnableSwagger", false))
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "LockHaven API v1");
                options.RoutePrefix = string.Empty;
                options.DocumentTitle = "LockHaven API Docs";
                options.DefaultModelsExpandDepth(-1);
            });
        }

        return app;
    }

    /// <summary>
    /// Maps all controller and system endpoints, including health checks.
    /// </summary>
    public static WebApplication MapLockHavenEndpoints(this WebApplication app)
    {
        app.MapControllers();

        // Liveness: lightweight process check
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        // Readiness: verifies DB and dependencies
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            ResponseWriter = HealthCheckResponseWriter.WriteDetailedResponse
        });

        // Diagnostic alias for manual checking
        app.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            ResponseWriter = HealthCheckResponseWriter.WriteDetailedResponse
        });

        return app;
    }

}
