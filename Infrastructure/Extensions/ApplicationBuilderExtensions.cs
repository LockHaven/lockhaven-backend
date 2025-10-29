using lockhaven_backend.Middleware;

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
    /// Maps controller endpoints for the LockHaven API.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> instance.</param>
    /// <returns>The same <see cref="WebApplication"/> instance for chaining.</returns>
    public static WebApplication MapLockHavenEndpoints(this WebApplication app)
    {
        app.MapControllers();
        return app;
    }
}
