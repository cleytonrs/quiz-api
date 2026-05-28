using Microsoft.AspNetCore.Mvc;
using Moq;
using QuizApp.Controllers;
using QuizApp.DTOs;
using QuizApp.Services;

namespace QuizApp.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _controller = new AuthController(_authServiceMock.Object);
    }

    [Fact]
    public async Task Register_ReturnsOkWithAuthResult()
    {
        var request = new RegisterRequest { Email = "test@example.com", Password = "password123" };
        var expected = new AuthResultDto { Token = "jwt-token", Email = "test@example.com" };
        _authServiceMock.Setup(s => s.RegisterAsync(request)).ReturnsAsync(expected);

        var result = await _controller.Register(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var authResult = Assert.IsType<AuthResultDto>(okResult.Value);
        Assert.Equal(expected.Token, authResult.Token);
        Assert.Equal(expected.Email, authResult.Email);
    }

    [Fact]
    public async Task Login_ReturnsOkWithAuthResult()
    {
        var request = new LoginRequest { Email = "test@example.com", Password = "password123" };
        var expected = new AuthResultDto { Token = "jwt-token", Email = "test@example.com" };
        _authServiceMock.Setup(s => s.LoginAsync(request)).ReturnsAsync(expected);

        var result = await _controller.Login(request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var authResult = Assert.IsType<AuthResultDto>(okResult.Value);
        Assert.Equal(expected.Token, authResult.Token);
        Assert.Equal(expected.Email, authResult.Email);
    }

    [Fact]
    public async Task Register_CallsAuthServiceRegisterAsync()
    {
        var request = new RegisterRequest { Email = "user@test.com", Password = "securepass" };
        _authServiceMock.Setup(s => s.RegisterAsync(request))
            .ReturnsAsync(new AuthResultDto { Token = "token", Email = "user@test.com" });

        await _controller.Register(request);

        _authServiceMock.Verify(s => s.RegisterAsync(request), Times.Once);
    }

    [Fact]
    public async Task Login_CallsAuthServiceLoginAsync()
    {
        var request = new LoginRequest { Email = "user@test.com", Password = "securepass" };
        _authServiceMock.Setup(s => s.LoginAsync(request))
            .ReturnsAsync(new AuthResultDto { Token = "token", Email = "user@test.com" });

        await _controller.Login(request);

        _authServiceMock.Verify(s => s.LoginAsync(request), Times.Once);
    }
}
