using lockhaven_backend.Infrastructure.Configuration;
using lockhaven_backend.Infrastructure.Extensions;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 250 * 1024 * 1024; // 250 MB
});

// Add services
builder.Services
    .Configure<FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 250 * 1024 * 1024; // 250 MB
    })
    .AddLockHavenServices(builder.Configuration)
    .AddLockHavenSwagger();

var app = builder.Build();

// Configure
app.UseLockHavenMiddleware();
app.MapLockHavenEndpoints();

app.Run();
