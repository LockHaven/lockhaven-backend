using System.ComponentModel.DataAnnotations;

namespace lockhaven_backend.Models.Requests;

public class UpdateEnvironmentRequest
{
    [StringLength(40)]
    public string? Name { get; set; }

    [StringLength(60)]
    public string? Slug { get; set; }
}
