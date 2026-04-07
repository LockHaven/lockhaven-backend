using System.Security.Cryptography;
using System.Text;
using lockhaven_backend.Constants;
using lockhaven_backend.Services.Streaming;

namespace lockhaven_backend.Tests.Unit.Streaming;

public class ChunkedAesGcmStreamsTests
{
    [Fact]
    public async Task EncryptThenDecrypt_RoundTripsMultiChunkPayload()
    {
        var key = RandomNumberGenerator.GetBytes(EncryptionConstants.EncryptionKeySize);
        var iv = RandomNumberGenerator.GetBytes(EncryptionConstants.NonceSize);
        var plainText = Encoding.UTF8.GetBytes(new string('x', EncryptionConstants.ChunkSize + 42));

        await using var plainIn = new MemoryStream(plainText);
        using var encrypting = new ChunkedAesGcmEncryptingStream(plainIn, key, iv, leavePlaintextOpen: true);
        await using var cipherOut = new MemoryStream();
        await encrypting.CopyToAsync(cipherOut);
        cipherOut.Position = 0;

        using var decrypting = new ChunkedAesGcmDecryptingStream(cipherOut, key, iv, leaveCiphertextOpen: true);
        await using var plainOut = new MemoryStream();
        await decrypting.CopyToAsync(plainOut);

        Assert.Equal(plainText, plainOut.ToArray());
    }
}
