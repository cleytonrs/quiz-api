namespace QuizApp.Models;

public class QuizSession
{
    public Guid Id { get; set; }
    public int QuizId { get; set; }
    public string? UserId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int CorrectAnswerCount { get; set; }
    public int TotalQuestions { get; set; }
    public bool Passed { get; set; }
    public Quiz Quiz { get; set; } = null!;
    public ApplicationUser? User { get; set; }
    public ICollection<SessionAnswer> Answers { get; set; } = new List<SessionAnswer>();
}
