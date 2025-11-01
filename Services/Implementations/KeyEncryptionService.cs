using System.Security.Cryptography;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using lockhaven_backend.Services.Interfaces;

namespace lockhaven_backend.Services;

/// <summary>
/// Implements envelope encryption using Azure Key Vault.
/// Encrypts per-file data encryption keys (DEK) and initialization vectors (IV) 
/// with a Key Encryption Key (KEK) stored in Azure Key Vault.
/// </summary>
public class KeyEncryptionService : IKeyEncryptionService
{
    private readonly CryptographyClient _cryptographyClient;
    private readonly string _keyName;
    private const string KeyName = "lockhaven-file-encryption-key";

    public KeyEncryptionService(string keyVaultUrl, string? keyName = null)
    {
        if (string.IsNullOrWhiteSpace(keyVaultUrl))
        {
            throw new ArgumentException("Key Vault URL cannot be null or empty", nameof(keyVaultUrl));
        }

        _keyName = keyName ?? KeyName;

        // Use DefaultAzureCredential which supports multiple authentication methods:
        // 1. Environment variables (for local development)
        // 2. Managed Identity (for Azure services)
        // 3. Azure CLI (for local development)
        // 4. Visual Studio credentials
        var credential = new DefaultAzureCredential();
        var keyClient = new KeyClient(new Uri(keyVaultUrl), credential);

        // Ensure the key exists, create if it doesn't
        EnsureKeyExists(keyClient, _keyName).Wait();

        // Create cryptography client for encryption/decryption operations
        _cryptographyClient = new CryptographyClient(
            new Uri($"{keyVaultUrl.TrimEnd('/')}/keys/{_keyName}"),
            credential
        );
    }

    private async Task EnsureKeyExists(KeyClient keyClient, string keyName)
    {
        try
        {
            // Try to get the key
            await keyClient.GetKeyAsync(keyName);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            // Key doesn't exist, create it
            var createKeyOptions = new CreateRsaKeyOptions(keyName)
            {
                KeySize = 2048, // RSA-2048 for wrapping AES keys
            };
            await keyClient.CreateRsaKeyAsync(createKeyOptions);
        }
    }

    public async Task<string> EncryptKeyAsync(byte[] plaintextKey)
    {
        if (plaintextKey == null || plaintextKey.Length == 0)
        {
            throw new ArgumentException("Plaintext key cannot be null or empty", nameof(plaintextKey));
        }

        // Use RSA-OAEP to encrypt the AES key
        var encryptResult = await _cryptographyClient.EncryptAsync(
            EncryptionAlgorithm.RsaOaep,
            plaintextKey
        );

        return Convert.ToBase64String(encryptResult.Ciphertext);
    }

    public async Task<string> EncryptIvAsync(byte[] plaintextIv)
    {
        if (plaintextIv == null || plaintextIv.Length == 0)
        {
            throw new ArgumentException("Plaintext IV cannot be null or empty", nameof(plaintextIv));
        }

        // Use RSA-OAEP to encrypt the IV
        var encryptResult = await _cryptographyClient.EncryptAsync(
            EncryptionAlgorithm.RsaOaep,
            plaintextIv
        );

        return Convert.ToBase64String(encryptResult.Ciphertext);
    }

    public async Task<byte[]> DecryptKeyAsync(string encryptedKeyBase64)
    {
        if (string.IsNullOrWhiteSpace(encryptedKeyBase64))
        {
            throw new ArgumentException("Encrypted key cannot be null or empty", nameof(encryptedKeyBase64));
        }

        var encryptedKey = Convert.FromBase64String(encryptedKeyBase64);

        // Use RSA-OAEP to decrypt the AES key
        var decryptResult = await _cryptographyClient.DecryptAsync(
            EncryptionAlgorithm.RsaOaep,
            encryptedKey
        );

        return decryptResult.Plaintext;
    }

    public async Task<byte[]> DecryptIvAsync(string encryptedIvBase64)
    {
        if (string.IsNullOrWhiteSpace(encryptedIvBase64))
        {
            throw new ArgumentException("Encrypted IV cannot be null or empty", nameof(encryptedIvBase64));
        }

        var encryptedIv = Convert.FromBase64String(encryptedIvBase64);

        // Use RSA-OAEP to decrypt the IV
        var decryptResult = await _cryptographyClient.DecryptAsync(
            EncryptionAlgorithm.RsaOaep,
            encryptedIv
        );

        return decryptResult.Plaintext;
    }
}

