using System.ComponentModel.DataAnnotations;

namespace lockhaven_backend.Models;

public class User
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;    

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;   

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;  

    [Required]
    public string PasswordHash { get; set; } = string.Empty;    

    [Required]
    public Role Role { get; set; } = Role.User;   

    [Required]
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Free;

    [Required]
    public long CurrentStorageUsedBytes { get; set; } = 0;

    [Required]
    public int UploadsTodayCount { get; set; } = 0;

    public DateTime? UploadsCountDateUtc { get; set; }

    public int? CurrentSecretCount { get; set; }

    public DateTime? SecretsUpdatedAtUtc { get; set; }

    public virtual ICollection<File> Files { get; set; } = new List<File>();
    public virtual ICollection<Project> OwnedProjects { get; set; } = new List<Project>();
    public virtual ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();
    public virtual ICollection<SecretVersion> CreatedSecretVersions { get; set; } = new List<SecretVersion>();
    public virtual ICollection<SecretAuditEvent> SecretAuditEvents { get; set; } = new List<SecretAuditEvent>();

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLogin { get; set; }    
}