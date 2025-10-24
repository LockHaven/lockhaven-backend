using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Cryptography;

namespace lockhaven_backend.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, _logger);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex, ILogger logger)
    {
        var problem = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "An unexpected error occurred.",
            Detail = "Please try again later or contact support if the problem persists.",
            Status = (int)HttpStatusCode.InternalServerError
        };

        switch (ex)
        {
            case UnauthorizedAccessException:
                problem.Status = (int)HttpStatusCode.Unauthorized;
                problem.Title = "Unauthorized";
                problem.Detail = ex.Message;
                break;

            case ArgumentException or ArgumentNullException:
                problem.Status = (int)HttpStatusCode.BadRequest;
                problem.Title = "Invalid request";
                problem.Detail = ex.Message;
                break;

            case FileNotFoundException:
                problem.Status = (int)HttpStatusCode.NotFound;
                problem.Title = "Resource not found";
                problem.Detail = ex.Message;
                break;

            case IOException:
                problem.Status = (int)HttpStatusCode.ServiceUnavailable;
                problem.Title = "Storage unavailable";
                problem.Detail = "A storage error occurred. Please try again.";
                break;

            case DbUpdateConcurrencyException:
                problem.Status = (int)HttpStatusCode.Conflict;
                problem.Title = "Concurrency conflict";
                problem.Detail = "The resource was modified by another operation.";
                break;

            case DbUpdateException:
                problem.Status = (int)HttpStatusCode.InternalServerError;
                problem.Title = "Database error";
                problem.Detail = "A database error occurred while processing your request.";
                break;

            case FormatException or CryptographicException:
                problem.Status = (int)HttpStatusCode.InternalServerError;
                problem.Title = "Data integrity error";
                problem.Detail = "The file data could not be processed correctly.";
                break;

            default:
                logger.LogError(ex, "Unhandled exception occurred: {Message}", ex.Message);
                break;
        }

        logger.LogError(ex, "Exception handled by middleware: {Message}", ex.Message);

        context.Response.StatusCode = problem.Status ?? 500;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }
}
