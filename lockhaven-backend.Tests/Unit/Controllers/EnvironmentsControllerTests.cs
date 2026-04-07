using lockhaven_backend.Controllers;
using lockhaven_backend.Models.Requests;
using lockhaven_backend.Models.Responses;
using lockhaven_backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace lockhaven_backend.Tests.Unit.Controllers;

public class EnvironmentsControllerTests
{
    [Fact]
    public async Task GetEnvironments_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var envService = new Mock<IEnvironmentService>();
        envService
            .Setup(s => s.GetEnvironments(projectId, userId))
            .ReturnsAsync(new List<EnvironmentResponse>());
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var sut = new EnvironmentsController(envService.Object, currentUser.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await sut.GetEnvironments(projectId);

        Assert.IsType<OkObjectResult>(result);
        envService.Verify(s => s.GetEnvironments(projectId, userId), Times.Once);
    }

    [Fact]
    public async Task CreateEnvironment_Throws_WhenProjectIdEmpty()
    {
        var sut = new EnvironmentsController(Mock.Of<IEnvironmentService>(), Mock.Of<ICurrentUserService>())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        await Assert.ThrowsAsync<BadHttpRequestException>(() =>
            sut.CreateEnvironment(Guid.Empty, new CreateEnvironmentRequest { Name = "E", Slug = "e" }));
    }
}
