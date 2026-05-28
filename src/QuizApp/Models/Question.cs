using System.ComponentModel.DataAnnotations;

namespace QuizApp.Models;

public class Question : IValidatableObject
{
    public int Id { get; set; }
    public int QuizId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public Quiz Quiz { get; set; } = null!;
    public ICollection<AnswerOption> AnswerOptions { get; set; } = new List<AnswerOption>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AnswerOptions.Count != 4)
        {
            yield return new ValidationResult(
                "A question must have exactly 4 answer options.",
                new[] { nameof(AnswerOptions) });
        }

        if (AnswerOptions.Count(o => o.IsCorrect) != 1)
        {
            yield return new ValidationResult(
                "A question must have exactly 1 correct answer option.",
                new[] { nameof(AnswerOptions) });
        }
    }
}
