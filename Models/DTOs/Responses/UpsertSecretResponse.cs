namespace lockhaven_backend.Models.Responses;

public class UpsertSecretResponse
{
    public int CurrentVersion { get; set; }

    /// <summary>
    /// False when the plaintext matched the latest version and no new row was written.
    /// </summary>
    public bool CreatedNewVersion { get; set; }
}
