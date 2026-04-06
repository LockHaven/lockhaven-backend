namespace lockhaven_backend.Constants;

public static class AcceptedFileTypes
{
    // Absolute application ceiling; tier limits are enforced separately in FileService.
    public const long MaxUploadSizeBytes = SubscriptionLimits.PaidMaxFileSizeBytes;

    public static readonly HashSet<string> AllowedFileTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
    {
        "pdf", "doc", "docx", "txt", "xlsx", "xls", "csv", 
        "jpg", "jpeg", "png", "gif", "bmp", 
        "mp4", "mp3", "wav", 
        "json", "xml" //, "zip" NOTE: Disabled due to security concerns for now
    };
}

/** Notes on future zip support:
    - Strict upload size/rate limits
    - Antivirus/malware scanning (async is fine)
    - If extracting in future: safe extraction library, block .. paths, cap total extracted size/file count/nesting depth, block symlinks, reject encrypted archives unless required
**/