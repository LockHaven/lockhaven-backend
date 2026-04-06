using System.ComponentModel.DataAnnotations;
using lockhaven_backend.Constants;

namespace lockhaven_backend.Models.Requests;

public class UpdateEnvironmentRequest
{
    [StringLength(40)]
    public string? Name { get; set; }

    [StringLength(60)]
    [RegularExpression(SlugConstraints.OptionalPattern, ErrorMessage = "Slug must be URL-safe: lowercase letters, digits, and single hyphens between segments (e.g. staging).")]
    public string? Slug { get; set; }
}
