using lockhaven_backend.Models;
using lockhaven_backend.Models.Requests;
using lockhaven_backend.Services;
using lockhaven_backend.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace lockhaven_backend.Tests.Unit.Services;

public class ProjectServiceTests
{
    [Fact]
    public async Task CreateProject_PersistsValidSlug()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = ownerId,
            FirstName = "O",
            LastName = "W",
            Email = "owner@t.local",
            PasswordHash = "x",
            SubscriptionTier = SubscriptionTier.Free,
            UploadsCountDateUtc = DateTime.UtcNow.Date
        });
        await db.SaveChangesAsync();

        var sut = new ProjectService(db);
        var result = await sut.CreateProject(
            new CreateProjectRequest { Name = "App", Slug = "my-app" },
            ownerId);

        Assert.Equal("my-app", result.Slug);
        Assert.Equal(ProjectMemberRole.Owner, result.MyRole);
    }

    [Fact]
    public async Task CreateProject_ThrowsWhenSlugDuplicate()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = ownerId,
            FirstName = "O",
            LastName = "W",
            Email = "owner@t.local",
            PasswordHash = "x",
            SubscriptionTier = SubscriptionTier.Free,
            UploadsCountDateUtc = DateTime.UtcNow.Date
        });
        await db.SaveChangesAsync();

        var sut = new ProjectService(db);
        await sut.CreateProject(new CreateProjectRequest { Name = "A", Slug = "dup" }, ownerId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateProject(new CreateProjectRequest { Name = "B", Slug = "dup" }, ownerId));
    }

    [Fact]
    public async Task CreateProject_ThrowsWhenSlugNotUrlSafe()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = ownerId,
            FirstName = "O",
            LastName = "W",
            Email = "owner@t.local",
            PasswordHash = "x",
            SubscriptionTier = SubscriptionTier.Free,
            UploadsCountDateUtc = DateTime.UtcNow.Date
        });
        await db.SaveChangesAsync();

        var sut = new ProjectService(db);

        await Assert.ThrowsAsync<BadHttpRequestException>(() =>
            sut.CreateProject(new CreateProjectRequest { Name = "A", Slug = "bad slug" }, ownerId));
    }

    [Fact]
    public async Task UpdateProject_AsMember_ThrowsUnauthorized()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        db.Users.AddRange(
            new User
            {
                Id = ownerId,
                FirstName = "O",
                LastName = "W",
                Email = "owner@t.local",
                PasswordHash = "x",
                SubscriptionTier = SubscriptionTier.Free,
                UploadsCountDateUtc = DateTime.UtcNow.Date
            },
            new User
            {
                Id = memberId,
                FirstName = "M",
                LastName = "B",
                Email = "mem@t.local",
                PasswordHash = "x",
                SubscriptionTier = SubscriptionTier.Free,
                UploadsCountDateUtc = DateTime.UtcNow.Date
            });
        var project = new Project
        {
            OwnerUserId = ownerId,
            Name = "P",
            Slug = "proj",
            IsDeleted = false
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        db.ProjectMembers.Add(new ProjectMember
        {
            ProjectId = project.Id,
            UserId = memberId,
            Role = ProjectMemberRole.Member,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new ProjectService(db);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.UpdateProject(project.Id, new UpdateProjectRequest { Name = "X" }, memberId));
    }
}
