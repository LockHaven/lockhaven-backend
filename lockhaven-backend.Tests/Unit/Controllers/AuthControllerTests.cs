using lockhaven_backend.Controllers;
using lockhaven_backend.Models.Requests;
using lockhaven_backend.Models.Responses;
using lockhaven_backend.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace lockhaven_backend.Tests.Unit.Controllers;

public class AuthControllerTests
{
    [Fact]
    public async Task Register_ReturnsOk()
    {
        var auth = new Mock<IAuthService>();
        auth.Setup(a => a.Register(It.IsAny<RegisterRequest>()))
            .ReturnsAsync(new AuthResponse { Success = true, Message = "ok", Token = "t", User = new UserResponse() });

        var sut = new AuthController(auth.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await sut.Register(new RegisterRequest
        {
            FirstName = "A",
            LastName = "B",
            Email = "a@b.c",
            Password = "p"
        });

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsOk()
    {
        var auth = new Mock<IAuthService>();
        auth.Setup(a => a.Login(It.IsAny<LoginRequest>()))
            .ReturnsAsync(new AuthResponse { Success = true, Message = "ok", Token = "t", User = new UserResponse() });

        var sut = new AuthController(auth.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await sut.Login(new LoginRequest { Email = "a@b.c", Password = "p" });

        Assert.IsType<OkObjectResult>(result);
    }
}
