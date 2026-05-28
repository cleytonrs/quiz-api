namespace QuizApp.DTOs;

public record RegisterRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record AuthResultDto
{
    public string Token { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
