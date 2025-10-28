using System.Text;
using Azure.Storage.Blobs;
using lockhaven_backend.Data;
using lockhaven_backend.Filters;
using lockhaven_backend.Middleware;
using lockhaven_backend.Services;
using lockhaven_backend.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
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

    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
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

    // Configure file upload support
    options.OperationFilter<FileUploadOperation>();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured"))
            )
        };
    });

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton(sp => 
{
    var config = sp.GetRequiredService<IConfiguration>();
    var cs = config["BlobStorage:ConnectionString"] 
        ?? throw new InvalidOperationException("BlobStorage:ConnectionString is not configured");
    return new BlobServiceClient(cs);
});

// Use local file storage for development, Azure Blob Storage for production
// NOTE: REMOVE THIS CONDITION ONCE AZURE BLOB STORAGE IS UP
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddScoped<IBlobStorageService, LocalFileStorageService>(); // NEED TO ADD THIS BACK IN OR SETUP BLOB IN AZURE
}
else
{
    builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
}

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileService, FileService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "LockHaven API v1");
        options.RoutePrefix = string.Empty; // Makes Swagger UI available at the root (http://localhost:5155/)
        options.DocumentTitle = "LockHaven API Documentation";
        options.DefaultModelsExpandDepth(-1); // Hide schemas section by default
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseCors("AllowFrontend");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
