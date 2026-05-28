namespace QuizApp.Models;

public class SessionAnswer
{
    public int Id { get; set; }
    public Guid QuizSessionId { get; set; }
    public int QuestionId { get; set; }
    public int SelectedAnswerOptionId { get; set; }
    public bool IsCorrect { get; set; }
    public QuizSession QuizSession { get; set; } = null!;
    public Question Question { get; set; } = null!;
    public AnswerOption SelectedAnswerOption { get; set; } = null!;
}
