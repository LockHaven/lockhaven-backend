using lockhaven_backend.Controllers;
using lockhaven_backend.Models.Requests;
using lockhaven_backend.Models.Responses;
using lockhaven_backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace lockhaven_backend.Tests.Unit.Controllers;

public class SecretsControllerTests
{
    [Fact]
    public async Task GetSecret_ReturnsOk()
    {
        var projectId = Guid.NewGuid();
        var envId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var secretService = new Mock<ISecretService>();
        secretService
            .Setup(s => s.GetSecretAsync(projectId, envId, "K", userId, false, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetSecretResponse { Value = "x" });

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var sut = new SecretsController(secretService.Object, currentUser.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await sut.GetSecret(projectId, envId, "K");

        Assert.IsType<OkObjectResult>(result);
        secretService.Verify(
            s => s.GetSecretAsync(projectId, envId, "K", userId, false, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteSecret_ReturnsNoContent()
    {
        var projectId = Guid.NewGuid();
        var envId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var secretService = new Mock<ISecretService>();
        secretService
            .Setup(s => s.DeleteSecretAsync(projectId, envId, "K", userId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var sut = new SecretsController(secretService.Object, currentUser.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await sut.DeleteSecret(projectId, envId, "K");

        Assert.IsType<NoContentResult>(result);
    }
}
