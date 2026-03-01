using System.ComponentModel.DataAnnotations;

namespace lockhaven_backend.Models;

public class Secret
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid EnvironmentId { get; set; }

    [Required]
    [StringLength(200)]
    public string Key { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    public bool IsActive { get; set; } = true;

    [StringLength(100)]
    public string? ContentType { get; set; }

    public DateTime? LastRotatedAtUtc { get; set; }

    [Required]
    public int CurrentVersion { get; set; } = 1;

    [Required]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual ProjectEnvironment? Environment { get; set; }
    public virtual ICollection<SecretVersion> Versions { get; set; } = new List<SecretVersion>();
    public virtual ICollection<SecretAuditEvent> SecretAuditEvents { get; set; } = new List<SecretAuditEvent>();
}
