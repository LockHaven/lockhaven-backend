using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using lockhaven_backend.Services.Interfaces;

namespace lockhaven_backend.Services;

/// <summary>
/// Implements envelope encryption using Azure Key Vault.
/// Encrypts per-file Data Encryption Keys (DEKs) and IVs with a Key Encryption Key (KEK)
/// securely stored in Key Vault.
/// </summary>
public class KeyEncryptionService : IKeyEncryptionService
{
    private readonly CryptographyClient _cryptographyClient;
    private readonly KeyClient _keyClient;
    private readonly string _keyName;
    private const string DefaultKeyName = "lockhaven-file-encryption-key";

    public KeyEncryptionService(string keyVaultUrl, string? keyName = null)
    {
        if (string.IsNullOrWhiteSpace(keyVaultUrl))
            throw new ArgumentException("Key Vault URL cannot be null or empty.", nameof(keyVaultUrl));

        _keyName = keyName ?? DefaultKeyName;

        // DefaultAzureCredential works locally (Azure CLI, VS) and in production (Managed Identity)
        var credential = new DefaultAzureCredential();
        _keyClient = new KeyClient(new Uri(keyVaultUrl), credential);

        // Defer async key creation safely to avoid blocking startup
        _cryptographyClient = InitializeCryptographyClientAsync().GetAwaiter().GetResult();
    }

    private async Task<CryptographyClient> InitializeCryptographyClientAsync()
    {
        var key = await EnsureKeyExistsAsync();
        return new CryptographyClient(key.Id, new DefaultAzureCredential());
    }

    private async Task<KeyVaultKey> EnsureKeyExistsAsync()
    {
        try
        {
            return await _keyClient.GetKeyAsync(_keyName);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            // Key doesn't exist, create it once
            var createOptions = new CreateRsaKeyOptions(_keyName)
            {
                KeySize = 2048,
                Enabled = true
            };

            var newKey = await _keyClient.CreateRsaKeyAsync(createOptions);
            return newKey.Value;
        }
    }

    public async Task<string> EncryptKeyAsync(byte[] plaintextKey)
    {
        if (plaintextKey == null || plaintextKey.Length == 0)
            throw new ArgumentException("Plaintext key cannot be null or empty.", nameof(plaintextKey));

        var result = await _cryptographyClient.EncryptAsync(EncryptionAlgorithm.RsaOaep, plaintextKey);
        return Convert.ToBase64String(result.Ciphertext);
    }

    public async Task<string> EncryptIvAsync(byte[] plaintextIv)
    {
        if (plaintextIv == null || plaintextIv.Length == 0)
            throw new ArgumentException("Plaintext IV cannot be null or empty.", nameof(plaintextIv));

        var result = await _cryptographyClient.EncryptAsync(EncryptionAlgorithm.RsaOaep, plaintextIv);
        return Convert.ToBase64String(result.Ciphertext);
    }

    public async Task<byte[]> DecryptKeyAsync(string encryptedKeyBase64)
    {
        if (string.IsNullOrWhiteSpace(encryptedKeyBase64))
            throw new ArgumentException("Encrypted key cannot be null or empty.", nameof(encryptedKeyBase64));

        var encryptedKey = Convert.FromBase64String(encryptedKeyBase64);
        var result = await _cryptographyClient.DecryptAsync(EncryptionAlgorithm.RsaOaep, encryptedKey);
        return result.Plaintext;
    }

    public async Task<byte[]> DecryptIvAsync(string encryptedIvBase64)
    {
        if (string.IsNullOrWhiteSpace(encryptedIvBase64))
            throw new ArgumentException("Encrypted IV cannot be null or empty.", nameof(encryptedIvBase64));

        var encryptedIv = Convert.FromBase64String(encryptedIvBase64);
        var result = await _cryptographyClient.DecryptAsync(EncryptionAlgorithm.RsaOaep, encryptedIv);
        return result.Plaintext;
    }
}
