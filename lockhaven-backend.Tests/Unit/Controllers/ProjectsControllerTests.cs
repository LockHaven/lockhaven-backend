using lockhaven_backend.Controllers;
using lockhaven_backend.Models;
using lockhaven_backend.Models.Requests;
using lockhaven_backend.Models.Responses;
using lockhaven_backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace lockhaven_backend.Tests.Unit.Controllers;

public class ProjectsControllerTests
{
    [Fact]
    public async Task GetProjects_ReturnsOkWithPayload()
    {
        var userId = Guid.NewGuid();
        var projectService = new Mock<IProjectService>();
        projectService
            .Setup(s => s.GetProjects(userId))
            .ReturnsAsync(new List<ProjectResponse>());
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var sut = new ProjectsController(projectService.Object, currentUser.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await sut.GetProjects();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        projectService.Verify(s => s.GetProjects(userId), Times.Once);
    }

    [Fact]
    public async Task CreateProject_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var projectService = new Mock<IProjectService>();
        projectService
            .Setup(s => s.CreateProject(It.IsAny<CreateProjectRequest>(), userId))
            .ReturnsAsync(new ProjectResponse
            {
                Id = Guid.NewGuid(),
                OwnerUserId = userId,
                Name = "N",
                Slug = "n",
                MyRole = ProjectMemberRole.Owner,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var sut = new ProjectsController(projectService.Object, currentUser.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await sut.CreateProject(new CreateProjectRequest { Name = "N", Slug = "n" });

        Assert.IsType<OkObjectResult>(result);
    }
}
