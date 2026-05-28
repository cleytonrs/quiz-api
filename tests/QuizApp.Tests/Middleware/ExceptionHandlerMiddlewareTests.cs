using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using QuizApp.Exceptions;
using QuizApp.Middleware;

namespace QuizApp.Tests.Middleware;

public class ExceptionHandlerMiddlewareTests
{
    private readonly Mock<ILogger<ExceptionHandlerMiddleware>> _loggerMock;

    public ExceptionHandlerMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ExceptionHandlerMiddleware>>();
    }

    private (ExceptionHandlerMiddleware middleware, DefaultHttpContext context) CreateMiddleware(Exception? exceptionToThrow)
    {
        RequestDelegate next = exceptionToThrow != null
            ? _ => throw exceptionToThrow
            : _ => Task.CompletedTask;

        var middleware = new ExceptionHandlerMiddleware(next, _loggerMock.Object);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        return (middleware, context);
    }

    private async Task<JsonDocument> GetResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        return JsonDocument.Parse(body);
    }

    [Fact]
    public async Task InvokeAsync_NoException_PassesThrough()
    {
        var (middleware, context) = CreateMiddleware(null);

        await middleware.InvokeAsync(context);

        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_NotFoundException_Returns404WithErrorMessage()
    {
        var (middleware, context) = CreateMiddleware(new NotFoundException("Quiz not found"));

        await middleware.InvokeAsync(context);

        Assert.Equal(404, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        var json = await GetResponseBody(context);
        Assert.Equal("Quiz not found", json.RootElement.GetProperty("error").GetString());
        Assert.False(json.RootElement.TryGetProperty("details", out _));
    }

    [Fact]
    public async Task InvokeAsync_ConflictException_Returns409WithErrorMessage()
    {
        var (middleware, context) = CreateMiddleware(new ConflictException("Email is already in use"));

        await middleware.InvokeAsync(context);

        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        var json = await GetResponseBody(context);
        Assert.Equal("Email is already in use", json.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedException_Returns401WithErrorMessage()
    {
        var (middleware, context) = CreateMiddleware(new UnauthorizedException("Invalid email or password"));

        await middleware.InvokeAsync(context);

        Assert.Equal(401, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        var json = await GetResponseBody(context);
        Assert.Equal("Invalid email or password", json.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_Returns400WithDetailsArray()
    {
        var details = new[] { "'TopicName' must not be empty.", "'Questions' must have at least 1 item." };
        var (middleware, context) = CreateMiddleware(new ValidationException("Validation failed", details));

        await middleware.InvokeAsync(context);

        Assert.Equal(400, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        var json = await GetResponseBody(context);
        Assert.Equal("Validation failed", json.RootElement.GetProperty("error").GetString());

        var detailsArray = json.RootElement.GetProperty("details");
        Assert.Equal(2, detailsArray.GetArrayLength());
        Assert.Equal("'TopicName' must not be empty.", detailsArray[0].GetString());
        Assert.Equal("'Questions' must have at least 1 item.", detailsArray[1].GetString());
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_Returns500WithGenericMessage()
    {
        var (middleware, context) = CreateMiddleware(new InvalidOperationException("Something broke internally"));

        await middleware.InvokeAsync(context);

        Assert.Equal(500, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        var json = await GetResponseBody(context);
        Assert.Equal("An unexpected error occurred", json.RootElement.GetProperty("error").GetString());
        Assert.False(json.RootElement.TryGetProperty("details", out _));
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_LogsError()
    {
        var exception = new InvalidOperationException("Something broke internally");
        var (middleware, context) = CreateMiddleware(exception);

        await middleware.InvokeAsync(context);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_AppExceptionWithCustomStatusCode_ReturnsCorrectStatusCode()
    {
        var (middleware, context) = CreateMiddleware(new AppException("Forbidden", 403));

        await middleware.InvokeAsync(context);

        Assert.Equal(403, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);

        var json = await GetResponseBody(context);
        Assert.Equal("Forbidden", json.RootElement.GetProperty("error").GetString());
    }
}
