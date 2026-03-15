using System.ComponentModel.DataAnnotations;

namespace lockhaven_backend.Models.Requests;

public class CreateEnvironmentRequest
{
    [Required]
    [StringLength(40)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(60)]
    public string Slug { get; set; } = string.Empty;
}
