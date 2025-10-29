using lockhaven_backend.Infrastructure.Configuration;
using lockhaven_backend.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services
    .AddLockHavenServices(builder.Configuration, builder.Environment)
    .AddLockHavenSwagger();

var app = builder.Build();

// Configure
app.UseLockHavenMiddleware(app.Environment);
app.MapLockHavenEndpoints();

app.Run();
