using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using lockhaven_backend.Services.Interfaces;

namespace lockhaven_backend.Services;

public class VaultTransitKeyEncryptionService : IKeyEncryptionService
{
    private readonly HttpClient _httpClient;
    private readonly string _vaultAddress;
    private readonly string _transitKeyName;
    private readonly string _vaultToken;

    public VaultTransitKeyEncryptionService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _vaultAddress = configuration["Vault:Address"]?.TrimEnd('/')
            ?? throw new InvalidOperationException("Vault:Address is not configured");
        _transitKeyName = configuration["Vault:TransitKeyName"]
            ?? throw new InvalidOperationException("Vault:TransitKeyName is not configured");

        var configuredToken = configuration["Vault:Token"];
        var envToken = Environment.GetEnvironmentVariable("VAULT_TOKEN");
        _vaultToken = !string.IsNullOrWhiteSpace(configuredToken)
            ? configuredToken
            : !string.IsNullOrWhiteSpace(envToken)
                ? envToken
                : throw new InvalidOperationException("Vault token is not configured. Set Vault:Token or VAULT_TOKEN.");
    }

    public Task<string> EncryptKeyAsync(byte[] plaintextKey, CancellationToken cancellationToken = default)
        => EncryptAsync(plaintextKey, nameof(plaintextKey), cancellationToken);

    public Task<string> EncryptIvAsync(byte[] plaintextIv, CancellationToken cancellationToken = default)
        => EncryptAsync(plaintextIv, nameof(plaintextIv), cancellationToken);

    public Task<byte[]> DecryptKeyAsync(string encryptedKeyBase64, CancellationToken cancellationToken = default)
        => DecryptAsync(encryptedKeyBase64, nameof(encryptedKeyBase64), cancellationToken);

    public Task<byte[]> DecryptIvAsync(string encryptedIvBase64, CancellationToken cancellationToken = default)
        => DecryptAsync(encryptedIvBase64, nameof(encryptedIvBase64), cancellationToken);

    private async Task<string> EncryptAsync(byte[] plaintext, string paramName, CancellationToken cancellationToken)
    {
        if (plaintext == null || plaintext.Length == 0)
        {
            throw new ArgumentException("Plaintext value cannot be null or empty.", paramName);
        }

        var plaintextBase64 = Convert.ToBase64String(plaintext);
        var payload = JsonSerializer.Serialize(new { plaintext = plaintextBase64 });

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_vaultAddress}/v1/transit/encrypt/{_transitKeyName}")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _vaultToken);
        request.Headers.Add("X-Vault-Token", _vaultToken);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Vault transit encrypt failed ({(int)response.StatusCode}): {responseBody}");
        }

        using var jsonDoc = JsonDocument.Parse(responseBody);
        var ciphertext = jsonDoc.RootElement
            .GetProperty("data")
            .GetProperty("ciphertext")
            .GetString();

        if (string.IsNullOrWhiteSpace(ciphertext))
        {
            throw new InvalidOperationException("Vault transit encrypt response did not contain ciphertext.");
        }

        return ciphertext;
    }

    private async Task<byte[]> DecryptAsync(string ciphertext, string paramName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ciphertext))
        {
            throw new ArgumentException("Ciphertext cannot be null or empty.", paramName);
        }

        var payload = JsonSerializer.Serialize(new { ciphertext });

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_vaultAddress}/v1/transit/decrypt/{_transitKeyName}")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _vaultToken);
        request.Headers.Add("X-Vault-Token", _vaultToken);

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Vault transit decrypt failed ({(int)response.StatusCode}): {responseBody}");
        }

        using var jsonDoc = JsonDocument.Parse(responseBody);
        var plaintextBase64 = jsonDoc.RootElement
            .GetProperty("data")
            .GetProperty("plaintext")
            .GetString();

        if (string.IsNullOrWhiteSpace(plaintextBase64))
        {
            throw new InvalidOperationException("Vault transit decrypt response did not contain plaintext.");
        }

        return Convert.FromBase64String(plaintextBase64);
    }
}
