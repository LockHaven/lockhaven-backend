using System.ComponentModel.DataAnnotations;

namespace lockhaven_backend.Models.Requests;

public class CreateProjectRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Slug { get; set; } = string.Empty;
}
