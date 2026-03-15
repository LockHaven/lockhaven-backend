using System.ComponentModel.DataAnnotations;

namespace lockhaven_backend.Models.Requests;

public class UpdateProjectRequest
{
    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(120)]
    public string? Slug { get; set; }
}
