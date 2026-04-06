using System.ComponentModel.DataAnnotations;
using lockhaven_backend.Constants;

namespace lockhaven_backend.Models.Requests;

public class CreateEnvironmentRequest
{
    [Required]
    [StringLength(40)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    [RegularExpression(SlugConstraints.Pattern, ErrorMessage = "Slug must be URL-safe: lowercase letters, digits, and single hyphens between segments (e.g. staging).")]
    public string Slug { get; set; } = string.Empty;
}
