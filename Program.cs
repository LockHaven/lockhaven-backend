using lockhaven_backend.Infrastructure.Configuration;
using lockhaven_backend.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services
    .AddLockHavenServices(builder.Configuration)
    .AddLockHavenSwagger();

var app = builder.Build();

// Configure
app.UseLockHavenMiddleware();
app.MapLockHavenEndpoints();

app.Run();
