namespace lockhaven_backend.Services.Interfaces;

/// <summary>
/// Service for envelope encryption of data encryption keys (DEK) and initialization vectors (IV).
/// Uses a Key Encryption Key (KEK) stored in Azure Key Vault to encrypt/decrypt per-file keys.
/// </summary>
public interface IKeyEncryptionService
{
    /// <summary>
    /// Encrypts a data encryption key (DEK) using the Key Encryption Key (KEK) from Azure Key Vault.
    /// </summary>
    /// <param name="plaintextKey">The plaintext AES key (DEK) to encrypt</param>
    /// <returns>The encrypted key as a base64 string</returns>
    Task<string> EncryptKeyAsync(byte[] plaintextKey);

    /// <summary>
    /// Encrypts an initialization vector (IV) using the Key Encryption Key (KEK) from Azure Key Vault.
    /// </summary>
    /// <param name="plaintextIv">The plaintext IV to encrypt</param>
    /// <returns>The encrypted IV as a base64 string</returns>
    Task<string> EncryptIvAsync(byte[] plaintextIv);

    /// <summary>
    /// Decrypts an encrypted data encryption key (DEK) using the Key Encryption Key (KEK) from Azure Key Vault.
    /// </summary>
    /// <param name="encryptedKeyBase64">The encrypted key as a base64 string</param>
    /// <returns>The decrypted AES key (DEK)</returns>
    Task<byte[]> DecryptKeyAsync(string encryptedKeyBase64);

    /// <summary>
    /// Decrypts an encrypted initialization vector (IV) using the Key Encryption Key (KEK) from Azure Key Vault.
    /// </summary>
    /// <param name="encryptedIvBase64">The encrypted IV as a base64 string</param>
    /// <returns>The decrypted IV</returns>
    Task<byte[]> DecryptIvAsync(string encryptedIvBase64);
}

