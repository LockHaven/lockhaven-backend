using lockhaven_backend.Constants;
using lockhaven_backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace lockhaven_backend.Services;

public class FileValidationService : IFileValidationService
{
    private readonly bool _enableSignatureChecks;

    private static readonly Dictionary<string, HashSet<string>> AllowedMimeTypesByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pdf"] = new(StringComparer.OrdinalIgnoreCase) { "application/pdf" },
        ["doc"] = new(StringComparer.OrdinalIgnoreCase) { "application/msword" },
        ["docx"] = new(StringComparer.OrdinalIgnoreCase) { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        ["txt"] = new(StringComparer.OrdinalIgnoreCase) { "text/plain" },
        ["xlsx"] = new(StringComparer.OrdinalIgnoreCase) { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },
        ["xls"] = new(StringComparer.OrdinalIgnoreCase) { "application/vnd.ms-excel" },
        ["csv"] = new(StringComparer.OrdinalIgnoreCase) { "text/csv", "application/csv" },
        ["jpg"] = new(StringComparer.OrdinalIgnoreCase) { "image/jpeg" },
        ["jpeg"] = new(StringComparer.OrdinalIgnoreCase) { "image/jpeg" },
        ["png"] = new(StringComparer.OrdinalIgnoreCase) { "image/png" },
        ["gif"] = new(StringComparer.OrdinalIgnoreCase) { "image/gif" },
        ["bmp"] = new(StringComparer.OrdinalIgnoreCase) { "image/bmp" },
        ["mp4"] = new(StringComparer.OrdinalIgnoreCase) { "video/mp4" },
        ["mp3"] = new(StringComparer.OrdinalIgnoreCase) { "audio/mpeg" },
        ["wav"] = new(StringComparer.OrdinalIgnoreCase) { "audio/wav", "audio/wave", "audio/x-wav" },
        ["json"] = new(StringComparer.OrdinalIgnoreCase) { "application/json", "text/json" },
        ["xml"] = new(StringComparer.OrdinalIgnoreCase) { "application/xml", "text/xml" },
        //["zip"] = new(StringComparer.OrdinalIgnoreCase) { "application/zip", "application/x-zip-compressed", "multipart/x-zip" } NOTE: Disabled due to security concerns for now
    };

    public FileValidationService(IConfiguration configuration)
    {
        _enableSignatureChecks = configuration.GetValue("FileValidation:EnableSignatureChecks", true);
    }

    public void ValidateUpload(IFormFile file)
    {
        if (file == null)
        {
            throw new ArgumentNullException(nameof(file));
        }

        if (file.Length <= 0)
        {
            throw new BadHttpRequestException("File is null or empty");
        }

        if (file.Length > AcceptedFileTypes.MaxUploadSizeBytes)
        {
            throw new BadHttpRequestException($"File exceeds maximum size limit of {AcceptedFileTypes.MaxUploadSizeBytes / 1024 / 1024} MB");
        }

        var extension = Path.GetExtension(file.FileName)?.TrimStart('.').ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrEmpty(extension) || !AcceptedFileTypes.AllowedFileTypes.Contains(extension))
        {
            throw new BadHttpRequestException($"Unsupported file type: {extension}");
        }

        ValidateMimeType(file.ContentType, extension);
        ValidateFileSignature(file, extension, _enableSignatureChecks);
    }

    private static void ValidateMimeType(string? contentType, string extension)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new BadHttpRequestException("Content-Type header is required");
        }

        var normalizedContentType = contentType.Split(';', 2, StringSplitOptions.TrimEntries)[0];
        if (!AllowedMimeTypesByExtension.TryGetValue(extension, out var allowedMimeTypes))
        {
            return;
        }

        if (!allowedMimeTypes.Contains(normalizedContentType))
        {
            throw new BadHttpRequestException($"MIME type '{normalizedContentType}' is not allowed for .{extension} files");
        }
    }

    private static void ValidateFileSignature(IFormFile file, string extension, bool enableSignatureChecks)
    {
        if (!enableSignatureChecks)
        {
            return;
        }

        using var stream = file.OpenReadStream();
        if (!TryReadHeader(stream, 16, out var header))
        {
            throw new BadHttpRequestException("Unable to read file signature");
        }

        var signatureValid = extension.ToLowerInvariant() switch
        {
            "pdf" => StartsWith(header, 0x25, 0x50, 0x44, 0x46), // %PDF
            "jpg" or "jpeg" => StartsWith(header, 0xFF, 0xD8, 0xFF),
            "png" => StartsWith(header, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A),
            /**"zip" => StartsWith(header, 0x50, 0x4B, 0x03, 0x04)
                     || StartsWith(header, 0x50, 0x4B, 0x05, 0x06)
                     || StartsWith(header, 0x50, 0x4B, 0x07, 0x08),*/
            _ => true // Optional: no signature check for non-key formats
        };

        if (!signatureValid)
        {
            throw new BadHttpRequestException($"File content signature does not match .{extension} extension");
        }
    }

    private static bool TryReadHeader(Stream stream, int bytesToRead, out byte[] header)
    {
        header = new byte[bytesToRead];
        var totalRead = 0;

        while (totalRead < bytesToRead)
        {
            var read = stream.Read(header, totalRead, bytesToRead - totalRead);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        if (totalRead == 0)
        {
            return false;
        }

        if (totalRead < bytesToRead)
        {
            Array.Resize(ref header, totalRead);
        }

        return true;
    }

    private static bool StartsWith(byte[] bytes, params byte[] signature)
    {
        if (bytes.Length < signature.Length)
        {
            return false;
        }

        for (var i = 0; i < signature.Length; i++)
        {
            if (bytes[i] != signature[i])
            {
                return false;
            }
        }

        return true;
    }
}
