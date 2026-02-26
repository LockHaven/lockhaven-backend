using System.Net;
using System.Text;
using System.Text.Json;
using lockhaven_backend.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace lockhaven_backend.Tests;

public class VaultTransitKeyEncryptionServiceTests
{
    [Fact]
    public async Task EncryptThenDecrypt_RoundTripsBytes()
    {
        var service = CreateService(new VaultStubMessageHandler());
        var plaintext = Encoding.UTF8.GetBytes("roundtrip-key-material");

        var ciphertext = await service.EncryptKeyAsync(plaintext);
        var decrypted = await service.DecryptKeyAsync(ciphertext);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public async Task Encrypt_WhenVaultReturnsFailure_ThrowsInvalidOperationException()
    {
        var service = CreateService(new FailingVaultMessageHandler());

        var act = () => service.EncryptKeyAsync(Encoding.UTF8.GetBytes("abc"));

        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    private static VaultTransitKeyEncryptionService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new VaultTransitKeyEncryptionService(httpClient, BuildVaultConfiguration());
    }

    private static IConfiguration BuildVaultConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Vault:Address"] = "http://vault.local",
                ["Vault:TransitKeyName"] = "lockhaven-file-encryption-key",
                ["Vault:Token"] = "test-token"
            })
            .Build();

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

            if (path.Contains("/decrypt/", StringComparison.OrdinalIgnoreCase))
            {
                var ciphertext = requestJson.RootElement.GetProperty("ciphertext").GetString() ?? string.Empty;
                var plaintext = ciphertext.Replace("vault:v1:", string.Empty, StringComparison.Ordinal);
                var responseBody = JsonSerializer.Serialize(new
                {
                    data = new { plaintext }
                });
                return BuildJsonResponse(HttpStatusCode.OK, responseBody);
            }

            return BuildJsonResponse(HttpStatusCode.NotFound, "{\"errors\":[\"unknown path\"]}");
        }
    }

    private sealed class FailingVaultMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(BuildJsonResponse(HttpStatusCode.BadRequest, "{\"errors\":[\"forced failure\"]}"));
    }

    private static HttpResponseMessage BuildJsonResponse(HttpStatusCode statusCode, string body) =>
        new(statusCode)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
}
