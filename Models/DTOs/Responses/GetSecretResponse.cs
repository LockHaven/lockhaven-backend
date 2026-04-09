namespace lockhaven_backend.Models.Responses;

public class GetSecretResponse
{
    public string Value { get; set; } = string.Empty;

    public string? Key { get; set; }

    public int? CurrentVersion { get; set; }

    public int? VersionReturned { get; set; }

    public DateTime? CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
