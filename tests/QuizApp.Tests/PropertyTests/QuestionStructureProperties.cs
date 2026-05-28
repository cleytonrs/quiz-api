using System.ComponentModel.DataAnnotations;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using QuizApp.Models;

namespace QuizApp.Tests.PropertyTests;

/// <summary>
/// Property 1: Question Structure Invariant
/// For any question in the system, it SHALL have exactly four answer options,
/// and exactly one of those options SHALL be marked as correct.
///
/// Validates: Requirements 2.3, 10.2
/// </summary>
[Trait("Feature", "quiz-app")]
[Trait("Property", "1: Question Structure Invariant")]
public class QuestionStructureProperties
{
    /// <summary>
    /// **Validates: Requirements 2.3, 10.2**
    ///
    /// A question with exactly 4 answer options and exactly 1 correct answer
    /// must pass validation (no validation errors).
    /// </summary>
    [Property(Arbitrary = new[] { typeof(QuestionArbitraries) })]
    public bool ValidQuestion_WithFourOptionsAndOneCorrect_PassesValidation(
        ValidQuestionInput input)
    {
        var question = CreateQuestionWithOptions(input.OptionTexts, input.CorrectIndex);
        var validationResults = RunValidation(question);
        return !validationResults.Any();
    }

    /// <summary>
    /// **Validates: Requirements 2.3, 10.2**
    ///
    /// A question with a number of answer options other than 4 must fail validation
    /// with an error about the answer options count.
    /// </summary>
    [Property(Arbitrary = new[] { typeof(QuestionArbitraries) })]
    public bool Question_WithNonFourOptionCount_FailsValidation(
        InvalidOptionCountInput input)
    {
        var options = Enumerable.Range(0, input.OptionCount)
            .Select(i => new AnswerOption
            {
                Id = i + 1,
                Text = $"Option {i + 1}",
                IsCorrect = i == 0
            })
            .ToList();

        var question = new Question
        {
            Id = 1,
            QuizId = 1,
            Text = "Test question",
            OrderIndex = 1,
            AnswerOptions = options
        };

        var validationResults = RunValidation(question);
        return validationResults.Any(r => r.MemberNames.Contains(nameof(Question.AnswerOptions)));
    }

    /// <summary>
    /// **Validates: Requirements 2.3, 10.2**
    ///
    /// A question with exactly 4 options but not exactly 1 correct answer must fail
    /// validation with an error about the correct answer count.
    /// </summary>
    [Property(Arbitrary = new[] { typeof(QuestionArbitraries) })]
    public bool Question_WithFourOptionsButNotOneCorrect_FailsValidation(
        InvalidCorrectCountInput input)
    {
        var options = Enumerable.Range(0, 4)
            .Select(i => new AnswerOption
            {
                Id = i + 1,
                Text = $"Option {i + 1}",
                IsCorrect = input.CorrectFlags[i]
            })
            .ToList();

        var question = new Question
        {
            Id = 1,
            QuizId = 1,
            Text = "Test question",
            OrderIndex = 1,
            AnswerOptions = options
        };

        var validationResults = RunValidation(question);
        return validationResults.Any(r => r.MemberNames.Contains(nameof(Question.AnswerOptions)));
    }

    private static Question CreateQuestionWithOptions(string[] optionTexts, int correctIndex)
    {
        var options = optionTexts.Select((text, i) => new AnswerOption
        {
            Id = i + 1,
            Text = text,
            IsCorrect = i == correctIndex
        }).ToList();

        return new Question
        {
            Id = 1,
            QuizId = 1,
            Text = "Test question",
            OrderIndex = 1,
            AnswerOptions = options
        };
    }

    private static List<ValidationResult> RunValidation(Question question)
    {
        var context = new ValidationContext(question);
        var results = new List<ValidationResult>();
        // Call IValidatableObject.Validate directly to test the validation logic
        results.AddRange(question.Validate(context));
        return results;
    }
}

/// <summary>
/// Input type for valid question generation: exactly 4 options with 1 correct.
/// </summary>
public record ValidQuestionInput(string[] OptionTexts, int CorrectIndex);

/// <summary>
/// Input type for invalid option count generation: any count != 4.
/// </summary>
public record InvalidOptionCountInput(int OptionCount);

/// <summary>
/// Input type for invalid correct count generation: 4 options but != 1 correct.
/// </summary>
public record InvalidCorrectCountInput(bool[] CorrectFlags);

/// <summary>
/// Custom Arbitrary instances for question structure property tests.
/// </summary>
public static class QuestionArbitraries
{
    public static Arbitrary<ValidQuestionInput> ValidQuestionInput()
    {
        var nonEmptyStringGen = ArbMap.Default.GeneratorFor<NonEmptyString>()
            .Select(s => s.Get);

        var gen = Gen.ArrayOf(nonEmptyStringGen, 4)
            .SelectMany(texts => Gen.Choose(0, 3), (texts, correctIndex) =>
                new ValidQuestionInput(texts, correctIndex));

        return gen.ToArbitrary();
    }

    public static Arbitrary<InvalidOptionCountInput> InvalidOptionCountInput()
    {
        // Generate option counts that are NOT 4 (0-3 or 5-10)
        var gen = Gen.OneOf(
            Gen.Choose(0, 3),
            Gen.Choose(5, 10)
        ).Select(count => new InvalidOptionCountInput(count));

        return gen.ToArbitrary();
    }

    public static Arbitrary<InvalidCorrectCountInput> InvalidCorrectCountInput()
    {
        // Generate arrays of 4 bools where the count of true values is NOT 1
        // (i.e., 0, 2, 3, or 4 correct answers)
        var boolGen = ArbMap.Default.GeneratorFor<bool>();
        var gen = Gen.ArrayOf(boolGen, 4)
            .Where(flags => flags.Count(f => f) != 1)
            .Select(flags => new InvalidCorrectCountInput(flags));

        return gen.ToArbitrary();
    }
}
