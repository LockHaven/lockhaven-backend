using lockhaven_backend.Models;
using lockhaven_backend.Models.Requests;
using lockhaven_backend.Services;
using lockhaven_backend.Services.Interfaces;
using lockhaven_backend.Tests.TestInfrastructure;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace lockhaven_backend.Tests.Unit.Services;

public class AuthServiceTests
{
    [Fact]
    public async Task Register_CreatesUserAndReturnsToken()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var jwt = new Mock<IJwtService>();
        jwt.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("jwt-token");
        var sut = new AuthService(db, jwt.Object);

        var response = await sut.Register(new RegisterRequest
        {
            FirstName = "A",
            LastName = "B",
            Email = "new@t.local",
            Password = "secret123"
        });

        Assert.True(response.Success);
        Assert.Equal("jwt-token", response.Token);
        Assert.True(await db.Users.AnyAsync(u => u.Email == "new@t.local"));
    }

    [Fact]
    public async Task Register_ThrowsWhenEmailAlreadyExists()
    {
        await using var db = InMemoryDbContextFactory.Create();
        db.Users.Add(new User
        {
            FirstName = "X",
            LastName = "Y",
            Email = "dup@t.local",
            PasswordHash = "h",
            SubscriptionTier = SubscriptionTier.Free,
            UploadsCountDateUtc = DateTime.UtcNow.Date
        });
        await db.SaveChangesAsync();

        var sut = new AuthService(db, Mock.Of<IJwtService>());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.Register(new RegisterRequest
            {
                FirstName = "A",
                LastName = "B",
                Email = "dup@t.local",
                Password = "p"
            }));
    }

    [Fact]
    public async Task Login_ReturnsToken_WhenCredentialsValid()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var jwt = new Mock<IJwtService>();
        jwt.Setup(j => j.GenerateToken(It.IsAny<User>())).Returns("jwt-token");
        var sut = new AuthService(db, jwt.Object);

        await sut.Register(new RegisterRequest
        {
            FirstName = "A",
            LastName = "B",
            Email = "login@t.local",
            Password = "secret123"
        });

        var response = await sut.Login(new LoginRequest { Email = "login@t.local", Password = "secret123" });

        Assert.True(response.Success);
        Assert.Equal("jwt-token", response.Token);
    }

    [Fact]
    public async Task Login_Throws_WhenPasswordInvalid()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var sut = new AuthService(db, Mock.Of<IJwtService>());

        await sut.Register(new RegisterRequest
        {
            FirstName = "A",
            LastName = "B",
            Email = "login2@t.local",
            Password = "secret123"
        });

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            sut.Login(new LoginRequest { Email = "login2@t.local", Password = "wrong" }));
    }
}
