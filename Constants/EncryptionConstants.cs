namespace lockhaven_backend.Constants;

public static class EncryptionConstants
{
    /// <summary>
    /// AES-256 key size in bytes (32 bytes = 256 bits)
    /// </summary>
    public const int EncryptionKeySize = 32;
    
    /// <summary>
    /// GCM authentication tag size in bits (128 bits = 16 bytes)
    /// </summary>
    public const int TagSize = 16;
    
    /// <summary>
    /// GCM nonce/IV size in bytes (12 bytes = 96 bits)
    /// </summary>
    public const int NonceSize = 12;
}