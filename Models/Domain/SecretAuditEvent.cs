using System.ComponentModel.DataAnnotations;

namespace lockhaven_backend.Models;

public class SecretAuditEvent
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProjectId { get; set; }

    public Guid? EnvironmentId { get; set; }

    public Guid? SecretId { get; set; }

    public Guid? UserId { get; set; }

    [Required]
    [StringLength(50)]
    public string Action { get; set; } = string.Empty;

    [Required]
    public bool Success { get; set; }

    [StringLength(45)]
    public string? Ip { get; set; }

    [StringLength(256)]
    public string? UserAgent { get; set; }

    [Required]
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    public virtual Project? Project { get; set; }
    public virtual ProjectEnvironment? Environment { get; set; }
    public virtual Secret? Secret { get; set; }
    public virtual User? User { get; set; }
}
