using lockhaven_backend.Data;
using lockhaven_backend.Models;
using lockhaven_backend.Models.Requests;
using lockhaven_backend.Services;
using lockhaven_backend.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;

namespace lockhaven_backend.Tests.Unit.Services;

public class EnvironmentServiceTests
{
    [Fact]
    public async Task CreateEnvironment_PersistsSlug()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var project = await SeedProjectWithOwnerAsync(db, ownerId);

        var sut = new EnvironmentService(db);
        var env = await sut.CreateEnvironment(
            project.Id,
            new CreateEnvironmentRequest { Name = "Staging", Slug = "staging" },
            ownerId);

        Assert.Equal("staging", env.Slug);
    }

    [Fact]
    public async Task DeleteEnvironment_SoftDeletesEnvironmentAndSecrets()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var project = await SeedProjectWithOwnerAsync(db, ownerId);

        var env = new ProjectEnvironment
        {
            ProjectId = project.Id,
            Name = "Dev",
            Slug = "dev",
            IsDeleted = false
        };
        db.Environments.Add(env);
        await db.SaveChangesAsync();

        var secret = new Secret
        {
            EnvironmentId = env.Id,
            Key = "API_KEY",
            IsDeleted = false,
            CurrentVersion = 1
        };
        db.Secrets.Add(secret);
        await db.SaveChangesAsync();

        var sut = new EnvironmentService(db);
        await sut.DeleteEnvironment(project.Id, env.Id, ownerId);

        var reloadedEnv = await db.Environments.AsNoTracking().FirstAsync(e => e.Id == env.Id);
        var reloadedSecret = await db.Secrets.AsNoTracking().FirstAsync(s => s.Id == secret.Id);

        Assert.True(reloadedEnv.IsDeleted);
        Assert.NotNull(reloadedEnv.DeletedAtUtc);
        Assert.True(reloadedSecret.IsDeleted);
        Assert.NotNull(reloadedSecret.DeletedAtUtc);
    }

    private static async Task<Project> SeedProjectWithOwnerAsync(ApplicationDbContext db, Guid ownerId)
    {
        db.Users.Add(new User
        {
            Id = ownerId,
            FirstName = "O",
            LastName = "W",
            Email = $"owner-{ownerId:N}@t.local",
            PasswordHash = "x",
            SubscriptionTier = SubscriptionTier.Free,
            UploadsCountDateUtc = DateTime.UtcNow.Date
        });
        var project = new Project
        {
            OwnerUserId = ownerId,
            Name = "Proj",
            Slug = "proj",
            IsDeleted = false
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return project;
    }
}
