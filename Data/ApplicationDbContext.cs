using lockhaven_backend.Models;
using Microsoft.EntityFrameworkCore;
using File = lockhaven_backend.Models.File;

namespace lockhaven_backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
        : base(options) 
        {             
        }

    public DbSet<User> Users { get; set; }
    public DbSet<File> Files { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("dbo");

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<File>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.Size).IsRequired();
            entity.Property(e => e.ContentType).IsRequired();
            entity.Property(e => e.UploadedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.EncryptedKey).IsRequired();
            entity.Property(e => e.InitializationVector).IsRequired();
            entity.Property(e => e.BlobPath).IsRequired();
            entity.Property(e => e.UserId).IsRequired();
            
            // Configure relationship
            entity.HasOne(f => f.User)
                  .WithMany(u => u.Files)
                  .HasForeignKey(f => f.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}