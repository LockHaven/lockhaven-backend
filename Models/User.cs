using System.ComponentModel.DataAnnotations;

namespace lockhaven_backend.Models;

public class User
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;    

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;   

    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;  

    [Required]
    public string PasswordHash { get; set; } = string.Empty;    

    [Required]
    public Role Role { get; set; } = Role.User;    

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLogin { get; set; }    
}