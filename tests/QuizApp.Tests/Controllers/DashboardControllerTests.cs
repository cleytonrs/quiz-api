using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using QuizApp.Controllers;
using QuizApp.DTOs;
using QuizApp.Services;

namespace QuizApp.Tests.Controllers;

public class DashboardControllerTests
{
    private readonly Mock<IDashboardService> _dashboardServiceMock;
    private readonly DashboardController _controller;

    public DashboardControllerTests()
    {
        _dashboardServiceMock = new Mock<IDashboardService>();
        _controller = new DashboardController(_dashboardServiceMock.Object);
    }

    private void SetupUser(string userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private void SetupAnonymousUser()
    {
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task GetDashboard_ReturnsOkWithDashboardDto()
    {
        var userId = "user-123";
        SetupUser(userId);

        var expected = new DashboardDto
        {
            Sessions = new List<DashboardSessionDto>
            {
                new DashboardSessionDto
                {
                    SessionId = Guid.NewGuid(),
                    QuizTopicName = "C# Basics",
                    Score = 8,
                    TotalQuestions = 10,
                    Passed = true,
                    CompletedAt = DateTime.UtcNow
                }
            }
        };

        _dashboardServiceMock.Setup(s => s.GetDashboardAsync(userId)).ReturnsAsync(expected);

        var result = await _controller.GetDashboard();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dashboard = Assert.IsType<DashboardDto>(okResult.Value);
        Assert.Single(dashboard.Sessions);
        Assert.Equal("C# Basics", dashboard.Sessions[0].QuizTopicName);
    }

    [Fact]
    public async Task GetDashboard_CallsDashboardServiceWithCorrectUserId()
    {
        var userId = "user-456";
        SetupUser(userId);

        _dashboardServiceMock.Setup(s => s.GetDashboardAsync(userId))
            .ReturnsAsync(new DashboardDto());

        await _controller.GetDashboard();

        _dashboardServiceMock.Verify(s => s.GetDashboardAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetDashboard_ReturnsUnauthorized_WhenNoUserIdInClaims()
    {
        SetupAnonymousUser();

        var result = await _controller.GetDashboard();

        Assert.IsType<UnauthorizedResult>(result.Result);
    }

    [Fact]
    public async Task GetDashboard_UsesSubClaim_WhenNameIdentifierNotPresent()
    {
        var userId = "sub-user-789";
        var claims = new List<Claim>
        {
            new Claim("sub", userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _dashboardServiceMock.Setup(s => s.GetDashboardAsync(userId))
            .ReturnsAsync(new DashboardDto());

        var result = await _controller.GetDashboard();

        Assert.IsType<OkObjectResult>(result.Result);
        _dashboardServiceMock.Verify(s => s.GetDashboardAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetDashboard_ReturnsEmptyDashboard_WhenUserHasNoSessions()
    {
        var userId = "user-no-sessions";
        SetupUser(userId);

        _dashboardServiceMock.Setup(s => s.GetDashboardAsync(userId))
            .ReturnsAsync(new DashboardDto { Sessions = new List<DashboardSessionDto>() });

        var result = await _controller.GetDashboard();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dashboard = Assert.IsType<DashboardDto>(okResult.Value);
        Assert.Empty(dashboard.Sessions);
    }
}
