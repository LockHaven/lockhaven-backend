using System.ComponentModel.DataAnnotations;

namespace lockhaven_backend.Models;

public class Project
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string OwnerUserId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual User? OwnerUser { get; set; }
    public virtual ICollection<ProjectEnvironment> Environments { get; set; } = new List<ProjectEnvironment>();
    public virtual ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    public virtual ICollection<SecretAuditEvent> SecretAuditEvents { get; set; } = new List<SecretAuditEvent>();
}
