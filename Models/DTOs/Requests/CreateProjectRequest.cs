using System.ComponentModel.DataAnnotations;
using lockhaven_backend.Constants;

namespace lockhaven_backend.Models.Requests;

public class CreateProjectRequest
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    [RegularExpression(SlugConstraints.Pattern, ErrorMessage = "Slug must be URL-safe: lowercase letters, digits, and single hyphens between segments (e.g. my-project).")]
    public string Slug { get; set; } = string.Empty;
}
