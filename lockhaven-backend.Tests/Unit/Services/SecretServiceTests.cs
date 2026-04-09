using lockhaven_backend.Data;
using lockhaven_backend.Exceptions;
using lockhaven_backend.Models;
using lockhaven_backend.Models.Requests;
using lockhaven_backend.Services;
using lockhaven_backend.Services.Interfaces;
using lockhaven_backend.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace lockhaven_backend.Tests.Unit.Services;

public class SecretServiceTests
{
    [Fact]
    public async Task UpsertSecret_ThenGet_ReturnsPlaintext()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var (project, env) = await SeedProjectAndEnvironmentAsync(db, ownerId);

        var keyEnc = CreatePassThroughKeyEncryptionMock();
        var httpAccessor = new Mock<IHttpContextAccessor>();
        httpAccessor.Setup(h => h.HttpContext).Returns(new DefaultHttpContext());

        var sut = new SecretService(db, keyEnc.Object, httpAccessor.Object);

        await sut.UpsertSecretAsync(
            project.Id,
            env.Id,
            "my_key",
            new UpsertSecretRequest { Value = "secret-value" },
            ownerId);

        var got = await sut.GetSecretAsync(project.Id, env.Id, "my_key", ownerId, includeMetadata: false, version: null);

        Assert.Equal("secret-value", got.Value);
    }

    [Fact]
    public async Task UpsertSecret_SameValue_DoesNotBumpVersion()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var (project, env) = await SeedProjectAndEnvironmentAsync(db, ownerId);

        var keyEnc = CreatePassThroughKeyEncryptionMock();
        var httpAccessor = new Mock<IHttpContextAccessor>();
        httpAccessor.Setup(h => h.HttpContext).Returns(new DefaultHttpContext());

        var sut = new SecretService(db, keyEnc.Object, httpAccessor.Object);

        await sut.UpsertSecretAsync(
            project.Id,
            env.Id,
            "K",
            new UpsertSecretRequest { Value = "same" },
            ownerId);

        var second = await sut.UpsertSecretAsync(
            project.Id,
            env.Id,
            "K",
            new UpsertSecretRequest { Value = "same" },
            ownerId);

        Assert.False(second.CreatedNewVersion);
        Assert.Equal(1, second.CurrentVersion);

        var count = await db.SecretVersions.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task UpsertSecret_ReadOnlyMember_ThrowsForbidden()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var readOnlyId = Guid.NewGuid();
        var (project, env) = await SeedProjectAndEnvironmentAsync(db, ownerId);

        db.Users.Add(new User
        {
            Id = readOnlyId,
            FirstName = "R",
            LastName = "O",
            Email = $"ro-{readOnlyId:N}@t.local",
            PasswordHash = "x",
            SubscriptionTier = SubscriptionTier.Free,
            UploadsCountDateUtc = DateTime.UtcNow.Date
        });
        db.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = project.Id,
            UserId = readOnlyId,
            Role = ProjectMemberRole.ReadOnly,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var keyEnc = CreatePassThroughKeyEncryptionMock();
        var httpAccessor = new Mock<IHttpContextAccessor>();
        httpAccessor.Setup(h => h.HttpContext).Returns(new DefaultHttpContext());

        var sut = new SecretService(db, keyEnc.Object, httpAccessor.Object);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            sut.UpsertSecretAsync(
                project.Id,
                env.Id,
                "K",
                new UpsertSecretRequest { Value = "x" },
                readOnlyId));
    }

    [Fact]
    public async Task DeleteSecret_SoftDeletes()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var (project, env) = await SeedProjectAndEnvironmentAsync(db, ownerId);

        var keyEnc = CreatePassThroughKeyEncryptionMock();
        var httpAccessor = new Mock<IHttpContextAccessor>();
        httpAccessor.Setup(h => h.HttpContext).Returns(new DefaultHttpContext());

        var sut = new SecretService(db, keyEnc.Object, httpAccessor.Object);

        await sut.UpsertSecretAsync(
            project.Id,
            env.Id,
            "DEL",
            new UpsertSecretRequest { Value = "v" },
            ownerId);

        await sut.DeleteSecretAsync(project.Id, env.Id, "DEL", ownerId);

        var reloaded = await db.Secrets.AsNoTracking().SingleAsync(s => s.Key == "DEL");
        Assert.True(reloaded.IsDeleted);
        Assert.NotNull(reloaded.DeletedAtUtc);
    }

    private static Mock<IKeyEncryptionService> CreatePassThroughKeyEncryptionMock()
    {
        var keyEnc = new Mock<IKeyEncryptionService>();
        keyEnc.Setup(k => k.EncryptKeyAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[] k, CancellationToken _) => Convert.ToBase64String(k));
        keyEnc.Setup(k => k.DecryptKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string s, CancellationToken _) => Convert.FromBase64String(s));
        keyEnc.Setup(k => k.EncryptIvAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((byte[] iv, CancellationToken _) => Convert.ToBase64String(iv));
        keyEnc.Setup(k => k.DecryptIvAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string s, CancellationToken _) => Convert.FromBase64String(s));
        return keyEnc;
    }

    private static async Task<(Project project, ProjectEnvironment env)> SeedProjectAndEnvironmentAsync(
        ApplicationDbContext db,
        Guid ownerId)
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

        var env = new ProjectEnvironment
        {
            ProjectId = project.Id,
            Name = "Dev",
            Slug = "dev",
            IsDeleted = false
        };
        db.Environments.Add(env);
        await db.SaveChangesAsync();

        return (project, env);
    }
}
