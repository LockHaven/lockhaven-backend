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
    /// including exception handling, HTTPS redirection, authentication, and CORS.
    /// </summary>
    /// <param name="app">The <see cref="IApplicationBuilder"/> instance.</param>
    /// <param name="env">The current hosting environment.</param>
    /// <returns>The same <see cref="IApplicationBuilder"/> instance for chaining.</returns>
    public static IApplicationBuilder UseLockHavenMiddleware(this IApplicationBuilder app, IHostEnvironment env)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseCors("AllowFrontend");
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();

        if (env.IsDevelopment())
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

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            ResponseWriter = HealthCheckResponseWriter.WriteDetailedResponse
        });

        app.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            ResponseWriter = HealthCheckResponseWriter.WriteDetailedResponse
        });

        return app;
    }

}
