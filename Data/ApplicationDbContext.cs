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
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectEnvironment> Environments { get; set; }
    public DbSet<Secret> Secrets { get; set; }
    public DbSet<SecretVersion> SecretVersions { get; set; }
    public DbSet<ProjectMember> ProjectMembers { get; set; }
    public DbSet<SecretAuditEvent> SecretAuditEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.SubscriptionTier).IsRequired();
            entity.Property(e => e.CurrentStorageUsedBytes).IsRequired();
            entity.Property(e => e.UploadsTodayCount).IsRequired();
            entity.Property(e => e.CurrentSecretCount);
            entity.Property(e => e.SecretsUpdatedAtUtc);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<File>(entity =>
        {
            entity.ToTable("Files");
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

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("Projects");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OwnerUserId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(120);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.UpdatedAtUtc).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.DeletedAtUtc);

            entity.HasIndex(e => new { e.OwnerUserId, e.Slug })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");
            entity.HasIndex(e => new { e.OwnerUserId, e.Name })
                .HasFilter("\"IsDeleted\" = false");

            entity.HasOne(e => e.OwnerUser)
                  .WithMany(u => u.OwnedProjects)
                  .HasForeignKey(e => e.OwnerUserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProjectEnvironment>(entity =>
        {
            entity.ToTable("Environments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(40);
            entity.Property(e => e.Slug).IsRequired().HasMaxLength(60);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.UpdatedAtUtc).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.DeletedAtUtc);

            entity.HasIndex(e => new { e.ProjectId, e.Slug })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");

            entity.HasOne(e => e.Project)
                  .WithMany(p => p.Environments)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Secret>(entity =>
        {
            entity.ToTable("Secrets");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Key).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.CurrentVersion).IsRequired().HasDefaultValue(1);
            entity.Property(e => e.CreatedAtUtc).IsRequired();
            entity.Property(e => e.UpdatedAtUtc).IsRequired();
            entity.Property(e => e.IsDeleted).IsRequired().HasDefaultValue(false);
            entity.Property(e => e.DeletedAtUtc);

            entity.HasIndex(e => e.EnvironmentId);
            entity.HasIndex(e => new { e.EnvironmentId, e.Key })
                .IsUnique()
                .HasFilter("\"IsDeleted\" = false");

            entity.HasOne(e => e.Environment)
                  .WithMany(env => env.Secrets)
                  .HasForeignKey(e => e.EnvironmentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SecretVersion>(entity =>
        {
            entity.ToTable("SecretVersions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Version).IsRequired();
            entity.Property(e => e.EncryptedPayload).IsRequired();
            entity.Property(e => e.EncryptedDek).IsRequired();
            entity.Property(e => e.Iv).IsRequired();
            entity.Property(e => e.CreatedAtUtc).IsRequired();

            entity.HasIndex(e => e.SecretId);
            entity.HasIndex(e => new { e.SecretId, e.Version }).IsUnique();

            entity.HasOne(e => e.Secret)
                  .WithMany(s => s.Versions)
                  .HasForeignKey(e => e.SecretId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CreatedByUser)
                  .WithMany(u => u.CreatedSecretVersions)
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.ToTable("ProjectMembers");
            entity.HasKey(e => new { e.ProjectId, e.UserId });
            entity.Property(e => e.Role).IsRequired().HasConversion<short>().HasColumnType("smallint");
            entity.Property(e => e.CreatedAtUtc).IsRequired();

            entity.HasOne(e => e.Project)
                  .WithMany(p => p.Members)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.ProjectMemberships)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SecretAuditEvent>(entity =>
        {
            entity.ToTable("SecretAuditEvents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Success).IsRequired();
            entity.Property(e => e.Ip).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(256);
            entity.Property(e => e.OccurredAtUtc).IsRequired();

            entity.HasIndex(e => new { e.ProjectId, e.OccurredAtUtc });
            entity.HasIndex(e => new { e.SecretId, e.OccurredAtUtc });

            entity.HasOne(e => e.Project)
                  .WithMany(p => p.SecretAuditEvents)
                  .HasForeignKey(e => e.ProjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Environment)
                  .WithMany(env => env.SecretAuditEvents)
                  .HasForeignKey(e => e.EnvironmentId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Secret)
                  .WithMany(s => s.SecretAuditEvents)
                  .HasForeignKey(e => e.SecretId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.SecretAuditEvents)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}