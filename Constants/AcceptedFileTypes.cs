namespace lockhaven_backend.Constants;

public static class AcceptedFileTypes
{
    public const long MaxUploadSizeBytes = 5 * 1024 * 1024;

    public static readonly HashSet<string> AllowedFileTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
    {
        "pdf", "doc", "docx", "txt", "xlsx", "xls", "csv", 
        "jpg", "jpeg", "png", "gif", "bmp", 
        "mp4", "mp3", "wav", 
        "json", "xml", "zip"
    };
}