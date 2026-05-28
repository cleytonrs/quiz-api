namespace QuizApp.Models;

public class Quiz
{
    public int Id { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
