using System.ComponentModel.DataAnnotations;

namespace lockhaven_backend.Models.Requests;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}