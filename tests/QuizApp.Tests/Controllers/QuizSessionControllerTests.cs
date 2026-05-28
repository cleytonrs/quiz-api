using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using QuizApp.Controllers;
using QuizApp.DTOs;
using QuizApp.Services;

namespace QuizApp.Tests.Controllers;

public class QuizSessionControllerTests
{
    private readonly Mock<IQuizSessionService> _sessionServiceMock;
    private readonly QuizSessionController _controller;

    public QuizSessionControllerTests()
    {
        _sessionServiceMock = new Mock<IQuizSessionService>();
        _controller = new QuizSessionController(_sessionServiceMock.Object);
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

    private void SetupAuthenticatedUser(string userId)
    {
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task StartSession_Anonymous_CallsServiceWithNullUserId()
    {
        SetupAnonymousUser();
        var sessionDto = new QuizSessionDto
        {
            Id = Guid.NewGuid(),
            QuizId = 1,
            StartedAt = DateTime.UtcNow
        };
        _sessionServiceMock.Setup(s => s.StartSessionAsync(1, null)).ReturnsAsync(sessionDto);

        var result = await _controller.StartSession(1);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
        var returned = Assert.IsType<QuizSessionDto>(createdResult.Value);
        Assert.Equal(sessionDto.Id, returned.Id);
        _sessionServiceMock.Verify(s => s.StartSessionAsync(1, null), Times.Once);
    }

    [Fact]
    public async Task StartSession_Authenticated_CallsServiceWithUserId()
    {
        var userId = "user-123";
        SetupAuthenticatedUser(userId);
        var sessionDto = new QuizSessionDto
        {
            Id = Guid.NewGuid(),
            QuizId = 2,
            StartedAt = DateTime.UtcNow
        };
        _sessionServiceMock.Setup(s => s.StartSessionAsync(2, userId)).ReturnsAsync(sessionDto);

        var result = await _controller.StartSession(2);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returned = Assert.IsType<QuizSessionDto>(createdResult.Value);
        Assert.Equal(sessionDto.Id, returned.Id);
        _sessionServiceMock.Verify(s => s.StartSessionAsync(2, userId), Times.Once);
    }

    [Fact]
    public async Task SubmitAnswer_ReturnsOkWithAnswerResult()
    {
        SetupAnonymousUser();
        var sessionId = Guid.NewGuid();
        var request = new SubmitAnswerRequest { QuestionId = 5, SelectedAnswerOptionId = 12 };
        var answerResult = new AnswerResultDto { IsCorrect = true, CorrectAnswerOptionId = 12 };
        _sessionServiceMock.Setup(s => s.SubmitAnswerAsync(sessionId, request)).ReturnsAsync(answerResult);

        var result = await _controller.SubmitAnswer(sessionId, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<AnswerResultDto>(okResult.Value);
        Assert.True(returned.IsCorrect);
        Assert.Equal(12, returned.CorrectAnswerOptionId);
    }

    [Fact]
    public async Task CompleteSession_ReturnsOkWithQuizResult()
    {
        SetupAnonymousUser();
        var sessionId = Guid.NewGuid();
        var quizResult = new QuizResultDto
        {
            SessionId = sessionId,
            QuizTopicName = "C# Basics",
            TotalQuestions = 10,
            CorrectAnswers = 8,
            ScorePercentage = 80.0,
            Passed = true,
            ElapsedTimeSeconds = 120.5,
            CompletedAt = DateTime.UtcNow
        };
        _sessionServiceMock.Setup(s => s.CompleteSessionAsync(sessionId)).ReturnsAsync(quizResult);

        var result = await _controller.CompleteSession(sessionId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<QuizResultDto>(okResult.Value);
        Assert.Equal(sessionId, returned.SessionId);
        Assert.True(returned.Passed);
        Assert.Equal(80.0, returned.ScorePercentage);
    }

    [Fact]
    public async Task GetSessionResult_ReturnsOkWithQuizResult()
    {
        SetupAnonymousUser();
        var sessionId = Guid.NewGuid();
        var quizResult = new QuizResultDto
        {
            SessionId = sessionId,
            QuizTopicName = "JavaScript",
            TotalQuestions = 5,
            CorrectAnswers = 3,
            ScorePercentage = 60.0,
            Passed = false,
            ElapsedTimeSeconds = 90.0,
            CompletedAt = DateTime.UtcNow
        };
        _sessionServiceMock.Setup(s => s.GetSessionResultAsync(sessionId)).ReturnsAsync(quizResult);

        var result = await _controller.GetSessionResult(sessionId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returned = Assert.IsType<QuizResultDto>(okResult.Value);
        Assert.Equal(sessionId, returned.SessionId);
        Assert.False(returned.Passed);
        Assert.Equal(60.0, returned.ScorePercentage);
    }

    [Fact]
    public async Task StartSession_ReturnsCreatedAtActionWithCorrectRouteName()
    {
        SetupAnonymousUser();
        var sessionId = Guid.NewGuid();
        var sessionDto = new QuizSessionDto
        {
            Id = sessionId,
            QuizId = 1,
            StartedAt = DateTime.UtcNow
        };
        _sessionServiceMock.Setup(s => s.StartSessionAsync(1, null)).ReturnsAsync(sessionDto);

        var result = await _controller.StartSession(1);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(QuizSessionController.GetSessionResult), createdResult.ActionName);
        Assert.Equal(sessionId, createdResult.RouteValues!["sessionId"]);
    }
}
