namespace lockhaven_backend.Constants;

public static class AcceptedFileTypes
{
    public const long MaxUploadSizeBytes = 100 * 1024 * 1024; // 100MB

    public static readonly HashSet<string> AllowedFileTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
    {
        "pdf", "doc", "docx", "txt", "xlsx", "xls", "csv", 
        "jpg", "jpeg", "png", "gif", "bmp", 
        "mp4", "mp3", "wav", 
        "json", "xml" //, "zip" NOTE: Disabled due to security concerns for now
    };
}

/** Notes on future zip support:
    - Strict upload size/rate limits (you already have 5MB cap)
    - Antivirus/malware scanning (async is fine)
    - If extracting in future: safe extraction library, block .. paths, cap total extracted size/file count/nesting depth, block symlinks, reject encrypted archives unless required
**/