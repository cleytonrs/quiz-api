namespace QuizApp.DTOs;

public record QuizSummaryDto
{
    public int Id { get; init; }
    public string TopicName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int QuestionCount { get; init; }
}

public record QuizDetailDto
{
    public int Id { get; init; }
    public string TopicName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<QuestionDto> Questions { get; init; } = new();
}

public record QuestionDto
{
    public int Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public int OrderIndex { get; init; }
    public List<AnswerOptionDto> AnswerOptions { get; init; } = new();
}

public record AnswerOptionDto
{
    public int Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public bool IsCorrect { get; init; }
}

public record CreateQuizRequest
{
    public string TopicName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<CreateQuestionRequest> Questions { get; init; } = new();
}

public record CreateQuestionRequest
{
    public string Text { get; init; } = string.Empty;
    public List<CreateAnswerOptionRequest> AnswerOptions { get; init; } = new();
}

public record CreateAnswerOptionRequest
{
    public string Text { get; init; } = string.Empty;
    public bool IsCorrect { get; init; }
}

public record UpdateQuizRequest
{
    public string TopicName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<CreateQuestionRequest> Questions { get; init; } = new();
}
