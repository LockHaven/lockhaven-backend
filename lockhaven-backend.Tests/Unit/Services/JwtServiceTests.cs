using lockhaven_backend.Models;
using lockhaven_backend.Services;
using Microsoft.Extensions.Configuration;

namespace lockhaven_backend.Tests.Unit.Services;

public class JwtServiceTests
{
    private static IConfiguration BuildJwtConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = new string('k', 32),
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:TokenExpirationMinutes"] = "60",
                ["Jwt:RefreshTokenLength"] = "32"
            })
            .Build();

    [Fact]
    public void GenerateToken_ReturnsNonEmptyString()
    {
        var sut = new JwtService(BuildJwtConfiguration());
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "u@test.local",
            FirstName = "A",
            LastName = "B",
            Role = Role.User
        };

        var token = sut.GenerateToken(user);

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void GenerateRefreshToken_ReturnsBase64OfConfiguredLength()
    {
        var sut = new JwtService(BuildJwtConfiguration());
        var refresh = sut.GenerateRefreshToken();
        var bytes = Convert.FromBase64String(refresh);
        Assert.Equal(32, bytes.Length);
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_RoundTripsClaims()
    {
        var sut = new JwtService(BuildJwtConfiguration());
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "u@test.local",
            FirstName = "A",
            LastName = "B",
            Role = Role.User
        };
        var token = sut.GenerateToken(user);

        var principal = sut.GetPrincipalFromExpiredToken(token);

        var sub = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Assert.Equal(user.Id.ToString(), sub);
    }
}
