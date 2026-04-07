using System.Net;
using System.Text.Json;
using lockhaven_backend.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace lockhaven_backend.Tests.Unit.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_Sets400_ForBadHttpRequestException()
    {
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new BadHttpRequestException("invalid"),
            NullLogger<ExceptionHandlingMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var doc = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("Invalid request format", doc.RootElement.GetProperty("title").GetString());
    }
}
