using System.Net;
using System.Text;
using System.Text.Json;
using lockhaven_backend.Data;
using lockhaven_backend.Models;
using lockhaven_backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace lockhaven_backend.Tests;

public class FileServiceSmokeTests
{
    [Fact]
    public async Task UploadAndDownload_WithLocalStorageAndVaultTransit_RoundTripsContent()
    {
        var storageRoot = Path.Combine(Directory.GetCurrentDirectory(), "local-storage");
        if (Directory.Exists(storageRoot))
        {
            Directory.Delete(storageRoot, true);
        }

        await using var dbContext = CreateDbContext();
        var validationService = new FileValidationService(BuildValidationConfiguration());
        var localStorage = new LocalFileStorageService(BuildValidationConfiguration(), NullLogger<LocalFileStorageService>.Instance);
        var vaultService = new VaultTransitKeyEncryptionService(
            new HttpClient(new VaultStubMessageHandler()),
            BuildVaultConfiguration());

        var fileService = new FileService(dbContext, localStorage, vaultService, validationService);

        var userId = Guid.NewGuid();
        dbContext.Users.Add(new User
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            Email = "smoke@test.local",
            PasswordHash = "x",
            SubscriptionTier = SubscriptionTier.Free,
            UploadsCountDateUtc = DateTime.UtcNow.Date
        });
        await dbContext.SaveChangesAsync();

        const string sourceContent = "lockhaven smoke test content";
        var uploadFile = CreateFormFile("smoke-test.txt", "text/plain", sourceContent);

        var saved = await fileService.UploadFile(uploadFile, userId);
        await using var downloaded = await fileService.DownloadFile(saved.Id, userId);
        using var reader = new StreamReader(downloaded, Encoding.UTF8);
        var downloadedContent = await reader.ReadToEndAsync();

        Assert.Equal(sourceContent, downloadedContent);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }

    private static IConfiguration BuildValidationConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileValidation:EnableSignatureChecks"] = "true"
            })
            .Build();

    private static IConfiguration BuildVaultConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Vault:Address"] = "http://vault.local",
                ["Vault:TransitKeyName"] = "lockhaven-file-encryption-key",
                ["Vault:Token"] = "test-token"
            })
            .Build();

    private static IFormFile CreateFormFile(string fileName, string contentType, string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class VaultStubMessageHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var requestBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            using var requestJson = JsonDocument.Parse(requestBody);
            var path = request.RequestUri!.AbsolutePath;

            if (path.Contains("/encrypt/", StringComparison.OrdinalIgnoreCase))
            {
                var plaintext = requestJson.RootElement.GetProperty("plaintext").GetString();
                var responseBody = JsonSerializer.Serialize(new
                {
                    data = new { ciphertext = $"vault:v1:{plaintext}" }
                });
                return BuildJsonResponse(HttpStatusCode.OK, responseBody);
            }

            var ciphertext = requestJson.RootElement.GetProperty("ciphertext").GetString() ?? string.Empty;
            var decoded = ciphertext.Replace("vault:v1:", string.Empty, StringComparison.Ordinal);
            var decryptResponse = JsonSerializer.Serialize(new
            {
                data = new { plaintext = decoded }
            });
            return BuildJsonResponse(HttpStatusCode.OK, decryptResponse);
        }
    }

    private static HttpResponseMessage BuildJsonResponse(HttpStatusCode statusCode, string body) =>
        new(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
}
