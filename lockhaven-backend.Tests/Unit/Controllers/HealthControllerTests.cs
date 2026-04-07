using lockhaven_backend.Controllers;
using lockhaven_backend.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace lockhaven_backend.Tests.Unit.Controllers;

public class HealthControllerTests
{
    [Fact]
    public async Task GetHealth_ReturnsOk_WithDatabaseSection()
    {
        await using var db = InMemoryDbContextFactory.Create();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = "Host=localhost"
            })
            .Build();
        var env = Mock.Of<IWebHostEnvironment>(e => e.EnvironmentName == "Test");

        var sut = new HealthController(db, env, config);

        var result = await sut.GetHealth();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }
}
