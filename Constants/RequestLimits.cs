namespace lockhaven_backend.Constants;

/// <summary>
/// Global HTTP request size defaults. Large uploads must opt in per endpoint (see file upload).
/// </summary>
public static class RequestLimits
{
    /// <summary>Default max request body for typical JSON/API traffic; not used for multipart file upload routes.</summary>
    public const long DefaultMaxRequestBodyBytes = 1024 * 1024; // 1 MB
}
