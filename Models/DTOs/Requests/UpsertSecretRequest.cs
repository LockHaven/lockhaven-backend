using System.ComponentModel.DataAnnotations;

namespace lockhaven_backend.Models.Requests;

public class UpsertSecretRequest
{
    [Required]
    public string Value { get; set; } = string.Empty;
}
