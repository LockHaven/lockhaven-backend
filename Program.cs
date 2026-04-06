using lockhaven_backend.Constants;
using lockhaven_backend.Infrastructure.Configuration;
using lockhaven_backend.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Conservative defaults — large bodies only where explicitly allowed (e.g. [RequestSizeLimit] on upload).
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = RequestLimits.DefaultMaxRequestBodyBytes;
});

builder.Services
    .Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = RequestLimits.DefaultMaxRequestBodyBytes;
    })
    .AddLockHavenServices(builder.Configuration)
    .AddLockHavenSwagger();

var app = builder.Build();

// Configure
app.UseLockHavenMiddleware();
app.MapLockHavenEndpoints();

app.Run();
