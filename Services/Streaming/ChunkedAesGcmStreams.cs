using System.Security.Cryptography;
using lockhaven_backend.Constants;

namespace lockhaven_backend.Services.Streaming;

/// <summary>
/// Reads plaintext from an inner stream, emits chunked AES-GCM ciphertext compatible with
/// <see cref="ChunkedAesGcmDecryptingStream"/> (Format V2: IV|tag|len|ciphertext per chunk).
/// Does not buffer the entire file in memory.
/// </summary>
public sealed class ChunkedAesGcmEncryptingStream : Stream
{
    private readonly Stream _plaintext;
    private readonly bool _leavePlaintextOpen;
    private readonly AesGcm _aesGcm;
    private readonly byte[] _baseIv;
    private readonly byte[] _plainChunkBuffer;

    private byte[]? _pending;
    private int _pendingOffset;
    private int _pendingLength;
    private int _chunkCounter;
    private bool _disposed;
    private bool _eof;

    public ChunkedAesGcmEncryptingStream(Stream plaintext, byte[] key, byte[] baseIv, bool leavePlaintextOpen = false)
    {
        _plaintext = plaintext ?? throw new ArgumentNullException(nameof(plaintext));
        _leavePlaintextOpen = leavePlaintextOpen;
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(baseIv);
        if (baseIv.Length != EncryptionConstants.NonceSize)
        {
            throw new ArgumentException($"Base IV must be {EncryptionConstants.NonceSize} bytes.", nameof(baseIv));
        }

        _aesGcm = new AesGcm(key, EncryptionConstants.TagSize);
        _baseIv = (byte[])baseIv.Clone();
        _plainChunkBuffer = new byte[EncryptionConstants.ChunkSize];
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() { }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ChunkedAesGcmEncryptingStream));
        }

        return ReadInternal(new Span<byte>(buffer, offset, count));
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ChunkedAesGcmEncryptingStream));
        }

        if (buffer.IsEmpty)
        {
            return ValueTask.FromResult(0);
        }

        return ReadAsyncCore(buffer, cancellationToken);
    }

    private async ValueTask<int> ReadAsyncCore(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            if (_pendingOffset < _pendingLength && _pending != null)
            {
                var n = Math.Min(buffer.Length - total, _pendingLength - _pendingOffset);
                _pending.AsSpan(_pendingOffset, n).CopyTo(buffer.Span.Slice(total, n));
                _pendingOffset += n;
                total += n;
                if (_pendingOffset >= _pendingLength)
                {
                    _pending = null;
                    _pendingOffset = 0;
                    _pendingLength = 0;
                }

                continue;
            }

            if (_eof)
            {
                return total;
            }

            var plainRead = await ReadPlainChunkAsync(cancellationToken).ConfigureAwait(false);
            if (plainRead == 0)
            {
                _eof = true;
                return total;
            }

            PackEncryptedChunk(plainRead);
        }

        return total;
    }

    private int ReadInternal(Span<byte> buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            if (_pendingOffset < _pendingLength && _pending != null)
            {
                var n = Math.Min(buffer.Length - total, _pendingLength - _pendingOffset);
                _pending.AsSpan(_pendingOffset, n).CopyTo(buffer.Slice(total, n));
                _pendingOffset += n;
                total += n;
                if (_pendingOffset >= _pendingLength)
                {
                    _pending = null;
                    _pendingOffset = 0;
                    _pendingLength = 0;
                }

                continue;
            }

            if (_eof)
            {
                return total;
            }

            var plainRead = ReadPlainChunkSync();
            if (plainRead == 0)
            {
                _eof = true;
                return total;
            }

            PackEncryptedChunk(plainRead);
        }

        return total;
    }

    private int ReadPlainChunkSync()
    {
        var totalRead = 0;
        while (totalRead < _plainChunkBuffer.Length)
        {
            var n = _plaintext.Read(_plainChunkBuffer, totalRead, _plainChunkBuffer.Length - totalRead);
            if (n == 0)
            {
                break;
            }

            totalRead += n;
        }

        return totalRead;
    }

    private async Task<int> ReadPlainChunkAsync(CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < _plainChunkBuffer.Length)
        {
            var n = await _plaintext
                .ReadAsync(_plainChunkBuffer.AsMemory(totalRead, _plainChunkBuffer.Length - totalRead), cancellationToken)
                .ConfigureAwait(false);
            if (n == 0)
            {
                break;
            }

            totalRead += n;
        }

        return totalRead;
    }

    private void PackEncryptedChunk(int plainBytes)
    {
        var chunkIv = new byte[EncryptionConstants.NonceSize];
        _baseIv.AsSpan().CopyTo(chunkIv);
        var counterBytes = BitConverter.GetBytes(_chunkCounter++);
        counterBytes.AsSpan().CopyTo(chunkIv.AsSpan(EncryptionConstants.NonceSize - 4, 4));

        var ciphertext = new byte[plainBytes];
        var tag = new byte[EncryptionConstants.TagSize];
        _aesGcm.Encrypt(chunkIv, _plainChunkBuffer.AsSpan(0, plainBytes), ciphertext, tag);

        var packedLength = EncryptionConstants.NonceSize + EncryptionConstants.TagSize + sizeof(int) + plainBytes;
        _pending = new byte[packedLength];
        var p = 0;
        chunkIv.CopyTo(_pending.AsSpan(p));
        p += EncryptionConstants.NonceSize;
        tag.CopyTo(_pending.AsSpan(p));
        p += EncryptionConstants.TagSize;
        BitConverter.TryWriteBytes(_pending.AsSpan(p), ciphertext.Length);
        p += sizeof(int);
        ciphertext.CopyTo(_pending.AsSpan(p));
        _pendingOffset = 0;
        _pendingLength = packedLength;
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _aesGcm.Dispose();
                if (!_leavePlaintextOpen)
                {
                    _plaintext.Dispose();
                }
            }

            _disposed = true;
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// Reads chunked AES-GCM ciphertext from an inner stream and exposes decrypted plaintext sequentially.
/// Does not buffer the entire file in memory.
/// </summary>
public sealed class ChunkedAesGcmDecryptingStream : Stream
{
    private readonly Stream _ciphertext;
    private readonly bool _leaveCiphertextOpen;
    private readonly AesGcm _aesGcm;

    private readonly byte[] _chunkIv;
    private readonly byte[] _tag;
    private readonly byte[] _lengthBytes;
    private byte[]? _plainPending;
    private int _plainOffset;
    private int _plainLength;
    private bool _disposed;
    private bool _eof;

    public ChunkedAesGcmDecryptingStream(Stream ciphertext, byte[] key, byte[] baseIv, bool leaveCiphertextOpen = false)
    {
        _ciphertext = ciphertext ?? throw new ArgumentNullException(nameof(ciphertext));
        _leaveCiphertextOpen = leaveCiphertextOpen;
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(baseIv);
        if (baseIv.Length != EncryptionConstants.NonceSize)
        {
            throw new ArgumentException($"Base IV must be {EncryptionConstants.NonceSize} bytes.", nameof(baseIv));
        }

        _aesGcm = new AesGcm(key, EncryptionConstants.TagSize);
        // Per-chunk IV is read from the ciphertext stream; baseIv is validated for API parity with envelope metadata.
        _chunkIv = new byte[EncryptionConstants.NonceSize];
        _tag = new byte[EncryptionConstants.TagSize];
        _lengthBytes = new byte[sizeof(int)];
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override void Flush() { }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArguments(buffer, offset, count);
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ChunkedAesGcmDecryptingStream));
        }

        return ReadInternal(new Span<byte>(buffer, offset, count));
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(ChunkedAesGcmDecryptingStream));
        }

        if (buffer.IsEmpty)
        {
            return ValueTask.FromResult(0);
        }

        return ReadAsyncCore(buffer, cancellationToken);
    }

    private async ValueTask<int> ReadAsyncCore(Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            if (_plainOffset < _plainLength && _plainPending != null)
            {
                var n = Math.Min(buffer.Length - total, _plainLength - _plainOffset);
                _plainPending.AsSpan(_plainOffset, n).CopyTo(buffer.Span.Slice(total, n));
                _plainOffset += n;
                total += n;
                if (_plainOffset >= _plainLength)
                {
                    _plainPending = null;
                    _plainOffset = 0;
                    _plainLength = 0;
                }

                continue;
            }

            if (_eof)
            {
                return total;
            }

            var decrypted = await ReadAndDecryptNextChunkAsync(cancellationToken).ConfigureAwait(false);
            if (decrypted == null)
            {
                _eof = true;
                return total;
            }

            _plainPending = decrypted;
            _plainOffset = 0;
            _plainLength = decrypted.Length;
        }

        return total;
    }

    private int ReadInternal(Span<byte> buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            if (_plainOffset < _plainLength && _plainPending != null)
            {
                var n = Math.Min(buffer.Length - total, _plainLength - _plainOffset);
                _plainPending.AsSpan(_plainOffset, n).CopyTo(buffer.Slice(total, n));
                _plainOffset += n;
                total += n;
                if (_plainOffset >= _plainLength)
                {
                    _plainPending = null;
                    _plainOffset = 0;
                    _plainLength = 0;
                }

                continue;
            }

            if (_eof)
            {
                return total;
            }

            var decrypted = ReadAndDecryptNextChunkSync();
            if (decrypted == null)
            {
                _eof = true;
                return total;
            }

            _plainPending = decrypted;
            _plainOffset = 0;
            _plainLength = decrypted.Length;
        }

        return total;
    }

    private byte[]? ReadAndDecryptNextChunkSync()
    {
        var ivRead = ReadExactlySync(_ciphertext, _chunkIv);
        if (ivRead == 0)
        {
            return null;
        }

        if (ivRead != _chunkIv.Length)
        {
            throw new CryptographicException("Invalid encrypted file format: truncated chunk IV");
        }

        if (ReadExactlySync(_ciphertext, _tag) != _tag.Length)
        {
            throw new CryptographicException("Invalid encrypted file format: truncated chunk tag");
        }

        if (ReadExactlySync(_ciphertext, _lengthBytes) != _lengthBytes.Length)
        {
            throw new CryptographicException("Invalid encrypted file format: truncated ciphertext length");
        }

        var ciphertextLength = BitConverter.ToInt32(_lengthBytes);
        if (ciphertextLength < 0 || ciphertextLength > EncryptionConstants.ChunkSize)
        {
            throw new CryptographicException($"Invalid encrypted file format: invalid ciphertext length {ciphertextLength}");
        }

        var ciphertext = new byte[ciphertextLength];
        if (ReadExactlySync(_ciphertext, ciphertext) != ciphertextLength)
        {
            throw new CryptographicException($"Invalid encrypted file format: truncated ciphertext (expected {ciphertextLength})");
        }

        var plaintext = new byte[ciphertextLength];
        _aesGcm.Decrypt(_chunkIv, ciphertext, _tag, plaintext);
        return plaintext;
    }

    private async Task<byte[]?> ReadAndDecryptNextChunkAsync(CancellationToken cancellationToken)
    {
        var ivRead = await ReadExactlyAsync(_ciphertext, _chunkIv, cancellationToken).ConfigureAwait(false);
        if (ivRead == 0)
        {
            return null;
        }

        if (ivRead != _chunkIv.Length)
        {
            throw new CryptographicException("Invalid encrypted file format: truncated chunk IV");
        }

        if (await ReadExactlyAsync(_ciphertext, _tag, cancellationToken).ConfigureAwait(false) != _tag.Length)
        {
            throw new CryptographicException("Invalid encrypted file format: truncated chunk tag");
        }

        if (await ReadExactlyAsync(_ciphertext, _lengthBytes, cancellationToken).ConfigureAwait(false) != _lengthBytes.Length)
        {
            throw new CryptographicException("Invalid encrypted file format: truncated ciphertext length");
        }

        var ciphertextLength = BitConverter.ToInt32(_lengthBytes);
        if (ciphertextLength < 0 || ciphertextLength > EncryptionConstants.ChunkSize)
        {
            throw new CryptographicException($"Invalid encrypted file format: invalid ciphertext length {ciphertextLength}");
        }

        var ciphertext = new byte[ciphertextLength];
        if (await ReadExactlyAsync(_ciphertext, ciphertext, cancellationToken).ConfigureAwait(false) != ciphertextLength)
        {
            throw new CryptographicException($"Invalid encrypted file format: truncated ciphertext (expected {ciphertextLength})");
        }

        var plaintext = new byte[ciphertextLength];
        _aesGcm.Decrypt(_chunkIv, ciphertext, _tag, plaintext);
        return plaintext;
    }

    private static int ReadExactlySync(Stream stream, Span<byte> buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = stream.Read(buffer[total..]);
            if (read == 0)
            {
                return total;
            }

            total += read;
        }

        return total;
    }

    private static async Task<int> ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), cancellationToken)
                .ConfigureAwait(false);
            if (read == 0)
            {
                return total;
            }

            total += read;
        }

        return total;
    }

    private static async Task<int> ReadExactlyAsync(Stream stream, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer[total..], cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return total;
            }

            total += read;
        }

        return total;
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _aesGcm.Dispose();
                if (!_leaveCiphertextOpen)
                {
                    _ciphertext.Dispose();
                }
            }

            _disposed = true;
        }

        base.Dispose(disposing);
    }
}
