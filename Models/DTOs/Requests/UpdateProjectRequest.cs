using System.ComponentModel.DataAnnotations;
using lockhaven_backend.Constants;

namespace lockhaven_backend.Models.Requests;

public class UpdateProjectRequest
{
    [StringLength(100)]
    public string? Name { get; set; }

    [StringLength(120)]
    [RegularExpression(SlugConstraints.OptionalPattern, ErrorMessage = "Slug must be URL-safe: lowercase letters, digits, and single hyphens between segments (e.g. my-project).")]
    public string? Slug { get; set; }
}
