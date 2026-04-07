using lockhaven_backend.Controllers;
using lockhaven_backend.Models;
using lockhaven_backend.Services.Interfaces;
using lockhaven_backend.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using FileModel = lockhaven_backend.Models.File;

namespace lockhaven_backend.Tests.Unit.Controllers;

public class FilesControllerTests
{
    [Fact]
    public async Task UploadFile_ReturnsOk_WhenServiceSucceeds()
    {
        var userId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var fileService = new Mock<IFileService>();
        fileService
            .Setup(f => f.UploadFile(It.IsAny<IFormFile>(), userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileModel
            {
                Id = fileId,
                Name = "a.txt",
                Size = 4,
                ContentType = "text/plain",
                UserId = userId,
                BlobPath = "x",
                EncryptedKey = "k",
                InitializationVector = "v",
                Type = FileType.Txt
            });
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var sut = new FilesController(fileService.Object, currentUser.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var formFile = TestFormFile.Create("a.txt", "text/plain", "data");
        var result = await sut.UploadFile(formFile);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetStorageUsed_ReturnsOk()
    {
        var userId = Guid.NewGuid();
        var fileService = new Mock<IFileService>();
        fileService.Setup(f => f.GetUserStorageUsed(userId)).ReturnsAsync(1024L);
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(userId);

        var sut = new FilesController(fileService.Object, currentUser.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await sut.GetStorageUsed();

        Assert.IsType<OkObjectResult>(result);
    }
}
