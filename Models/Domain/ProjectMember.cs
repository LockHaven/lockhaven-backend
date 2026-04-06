namespace lockhaven_backend.Models;

public class ProjectMember
{
    public Guid ProjectId { get; set; }

    public Guid UserId { get; set; }

    public ProjectMemberRole Role { get; set; } = ProjectMemberRole.Member;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public virtual Project? Project { get; set; }
    public virtual User? User { get; set; }
}
