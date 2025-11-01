using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace lockhaven_backend.Models;

public class File
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public FileType Type { get; set; }

    [Required]
    public long Size { get; set; }

    [Required]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string EncryptedKey { get; set; } = string.Empty; // Will be empty for client-side encryption

    [Required]
    public string InitializationVector { get; set; } = string.Empty; // Will be empty for client-side encryption

    [Required]
    public int EncryptionFormatVersion { get; set; } = 1; // Format version: 1 = single-IV, 2 = chunked

    [Required]
    public bool IsClientEncrypted { get; set; } = false; // Flag to indicate encryption method
    
    [Required]
    public bool IsShared { get; set; } = false; // Flag to indicate if file is shared
    
    public string? GroupId { get; set; } // For group/shared files

    [Required]
    public string BlobPath { get; set; } = string.Empty;

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public virtual User? User { get; set; }
}