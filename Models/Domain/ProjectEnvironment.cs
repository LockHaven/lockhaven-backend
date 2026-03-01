using System.ComponentModel.DataAnnotations;

namespace lockhaven_backend.Models;

public class ProjectEnvironment
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid ProjectId { get; set; }

    [Required]
    [StringLength(40)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual Project? Project { get; set; }
    public virtual ICollection<Secret> Secrets { get; set; } = new List<Secret>();
    public virtual ICollection<SecretAuditEvent> SecretAuditEvents { get; set; } = new List<SecretAuditEvent>();
}
