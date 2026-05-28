namespace QuizApp.DTOs;

public record DashboardDto
{
    public IReadOnlyList<DashboardSessionDto> Sessions { get; init; } = [];
}

public record DashboardSessionDto
{
    public Guid SessionId { get; init; }
    public string QuizTopicName { get; init; } = string.Empty;
    public int Score { get; init; }
    public int TotalQuestions { get; init; }
    public bool Passed { get; init; }
    public DateTime CompletedAt { get; init; }
}
