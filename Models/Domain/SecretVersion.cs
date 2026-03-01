using System.ComponentModel.DataAnnotations;

namespace lockhaven_backend.Models;

public class SecretVersion
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid SecretId { get; set; }

    [Required]
    public int Version { get; set; }

    [Required]
    public string EncryptedPayload { get; set; } = string.Empty;

    [Required]
    public string EncryptedDek { get; set; } = string.Empty;

    [Required]
    public string Iv { get; set; } = string.Empty;

    public string? PayloadHash { get; set; }

    public string? CreatedByUserId { get; set; }

    [Required]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual Secret? Secret { get; set; }
    public virtual User? CreatedByUser { get; set; }
}
