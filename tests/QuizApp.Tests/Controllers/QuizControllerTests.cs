using Microsoft.AspNetCore.Mvc;
using Moq;
using QuizApp.Controllers;
using QuizApp.DTOs;
using QuizApp.Services;

namespace QuizApp.Tests.Controllers;

public class QuizControllerTests
{
    private readonly Mock<IQuizService> _quizServiceMock;
    private readonly QuizController _controller;

    public QuizControllerTests()
    {
        _quizServiceMock = new Mock<IQuizService>();
        _controller = new QuizController(_quizServiceMock.Object);
    }

    [Fact]
    public async Task GetAllQuizzes_ReturnsOkWithQuizList()
    {
        var quizzes = new List<QuizSummaryDto>
        {
            new() { Id = 1, TopicName = "C#", Description = "C# basics", QuestionCount = 5 },
            new() { Id = 2, TopicName = "JavaScript", Description = "JS fundamentals", QuestionCount = 10 }
        };
        _quizServiceMock.Setup(s => s.GetAllQuizzesAsync()).ReturnsAsync(quizzes);

        var result = await _controller.GetAllQuizzes();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedQuizzes = Assert.IsAssignableFrom<IEnumerable<QuizSummaryDto>>(okResult.Value);
        Assert.Equal(2, returnedQuizzes.Count());
    }

    [Fact]
    public async Task GetQuizById_ReturnsOkWithQuizDetail()
    {
        var quiz = new QuizDetailDto
        {
            Id = 1,
            TopicName = "C#",
            Description = "C# basics",
            Questions = new List<QuestionDto>()
        };
        _quizServiceMock.Setup(s => s.GetQuizByIdAsync(1)).ReturnsAsync(quiz);

        var result = await _controller.GetQuizById(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedQuiz = Assert.IsType<QuizDetailDto>(okResult.Value);
        Assert.Equal(1, returnedQuiz.Id);
        Assert.Equal("C#", returnedQuiz.TopicName);
    }

    [Fact]
    public async Task CreateQuiz_ReturnsCreatedAtActionWithQuizDetail()
    {
        var request = new CreateQuizRequest
        {
            TopicName = "Python",
            Description = "Python basics",
            Questions = new List<CreateQuestionRequest>()
        };
        var created = new QuizDetailDto
        {
            Id = 3,
            TopicName = "Python",
            Description = "Python basics",
            Questions = new List<QuestionDto>()
        };
        _quizServiceMock.Setup(s => s.CreateQuizAsync(request)).ReturnsAsync(created);

        var result = await _controller.CreateQuiz(request);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(201, createdResult.StatusCode);
        Assert.Equal(nameof(QuizController.GetQuizById), createdResult.ActionName);
        var returnedQuiz = Assert.IsType<QuizDetailDto>(createdResult.Value);
        Assert.Equal(3, returnedQuiz.Id);
    }

    [Fact]
    public async Task UpdateQuiz_ReturnsOkWithUpdatedQuizDetail()
    {
        var request = new UpdateQuizRequest
        {
            TopicName = "C# Advanced",
            Description = "Advanced C# topics",
            Questions = new List<CreateQuestionRequest>()
        };
        var updated = new QuizDetailDto
        {
            Id = 1,
            TopicName = "C# Advanced",
            Description = "Advanced C# topics",
            Questions = new List<QuestionDto>()
        };
        _quizServiceMock.Setup(s => s.UpdateQuizAsync(1, request)).ReturnsAsync(updated);

        var result = await _controller.UpdateQuiz(1, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedQuiz = Assert.IsType<QuizDetailDto>(okResult.Value);
        Assert.Equal("C# Advanced", returnedQuiz.TopicName);
    }

    [Fact]
    public async Task DeleteQuiz_ReturnsNoContent()
    {
        _quizServiceMock.Setup(s => s.DeleteQuizAsync(1)).Returns(Task.CompletedTask);

        var result = await _controller.DeleteQuiz(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task CreateQuiz_CallsQuizServiceCreateAsync()
    {
        var request = new CreateQuizRequest
        {
            TopicName = "HTML",
            Description = "HTML basics",
            Questions = new List<CreateQuestionRequest>()
        };
        _quizServiceMock.Setup(s => s.CreateQuizAsync(request))
            .ReturnsAsync(new QuizDetailDto { Id = 1, TopicName = "HTML", Description = "HTML basics" });

        await _controller.CreateQuiz(request);

        _quizServiceMock.Verify(s => s.CreateQuizAsync(request), Times.Once);
    }

    [Fact]
    public async Task DeleteQuiz_CallsQuizServiceDeleteAsync()
    {
        _quizServiceMock.Setup(s => s.DeleteQuizAsync(5)).Returns(Task.CompletedTask);

        await _controller.DeleteQuiz(5);

        _quizServiceMock.Verify(s => s.DeleteQuizAsync(5), Times.Once);
    }
}
