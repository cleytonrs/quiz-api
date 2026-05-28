namespace QuizApp.DTOs;

public record QuizSessionDto
{
    public Guid Id { get; init; }
    public int QuizId { get; init; }
    public DateTime StartedAt { get; init; }
}

public record SubmitAnswerRequest
{
    public int QuestionId { get; init; }
    public int SelectedAnswerOptionId { get; init; }
}

public record AnswerResultDto
{
    public bool IsCorrect { get; init; }
    public int CorrectAnswerOptionId { get; init; }
}

public record QuizResultDto
{
    public Guid SessionId { get; init; }
    public string QuizTopicName { get; init; } = string.Empty;
    public int TotalQuestions { get; init; }
    public int CorrectAnswers { get; init; }
    public double ScorePercentage { get; init; }
    public bool Passed { get; init; }
    public double ElapsedTimeSeconds { get; init; }
    public DateTime CompletedAt { get; init; }
}
