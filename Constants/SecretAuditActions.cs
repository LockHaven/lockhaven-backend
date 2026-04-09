namespace lockhaven_backend.Constants;

/// <summary>
/// Values persisted on secret audit events (<c>Action</c> column).
/// </summary>
public static class SecretAuditActions
{
    public const string ReadValue = "SECRET_READ_VALUE";
    public const string Export = "SECRET_EXPORT";
    public const string Write = "SECRET_WRITE";
    public const string Delete = "SECRET_DELETE";
}
