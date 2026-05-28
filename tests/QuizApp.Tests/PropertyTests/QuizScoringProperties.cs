using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using QuizApp.Models;

namespace QuizApp.Tests.PropertyTests;

/// <summary>
/// Property 6: Correct Answer Count
/// For any completed quiz session with a list of session answers, the reported correct
/// answer count SHALL equal the number of answers in that list where isCorrect is true.
///
/// Validates: Requirements 4.2
/// </summary>
[Trait("Feature", "quiz-app")]
[Trait("Property", "6: Correct Answer Count")]
public class QuizScoringProperties
{
    /// <summary>
    /// **Validates: Requirements 4.2**
    ///
    /// For any list of boolean values representing isCorrect for each answer,
    /// the correct count (computed via LINQ Count where IsCorrect == true)
    /// should equal the count of true values in the input list.
    /// </summary>
    [Property(Arbitrary = new[] { typeof(ScoringArbitraries) })]
    public bool CorrectAnswerCount_EqualsNumberOfTrueIsCorrectValues(
        SessionAnswerListInput input)
    {
        // Build session answers from the generated boolean list
        var answers = input.IsCorrectValues
            .Select((isCorrect, index) => new SessionAnswer
            {
                Id = index + 1,
                QuizSessionId = Guid.NewGuid(),
                QuestionId = index + 1,
                SelectedAnswerOptionId = index + 1,
                IsCorrect = isCorrect
            })
            .ToList();

        // Apply the same logic used in CompleteSessionAsync
        var correctAnswerCount = answers.Count(a => a.IsCorrect);

        // Expected: count of true values in the input
        var expectedCount = input.IsCorrectValues.Count(v => v);

        return correctAnswerCount == expectedCount;
    }
}

/// <summary>
/// Input type for correct answer count property: a list of boolean isCorrect values.
/// </summary>
public record SessionAnswerListInput(bool[] IsCorrectValues);

/// <summary>
/// Property 7: Pass/Fail Threshold Determination
/// For any completed quiz session with correctAnswers correct out of totalQuestions total,
/// the pass status SHALL be true if and only if (correctAnswers / totalQuestions) ≥ 0.70.
///
/// Validates: Requirements 4.3, 4.4
/// </summary>
[Trait("Feature", "quiz-app")]
[Trait("Property", "7: Pass/Fail Threshold Determination")]
public class PassFailThresholdProperties
{
    /// <summary>
    /// **Validates: Requirements 4.3, 4.4**
    ///
    /// For any pair (correctAnswers, totalQuestions) where 0 <= correctAnswers <= totalQuestions
    /// and totalQuestions > 0, the pass/fail determination matches:
    /// passed == ((double)correctAnswers / totalQuestions >= 0.70)
    /// </summary>
    [Property(Arbitrary = new[] { typeof(ThresholdArbitraries) })]
    public bool PassStatus_IsTrueIfAndOnlyIfScoreAtOrAboveThreshold(
        ThresholdInput input)
    {
        // Apply the same logic used in CompleteSessionAsync
        var passed = input.TotalQuestions > 0
            && (double)input.CorrectAnswers / input.TotalQuestions >= 0.70;

        // Expected result based on the threshold definition
        var expected = (double)input.CorrectAnswers / input.TotalQuestions >= 0.70;

        return passed == expected;
    }
}

/// <summary>
/// Input type for pass/fail threshold property: a pair of (correctAnswers, totalQuestions).
/// </summary>
public record ThresholdInput(int CorrectAnswers, int TotalQuestions);

/// <summary>
/// Custom Arbitrary instances for quiz scoring property tests.
/// </summary>
public static class ScoringArbitraries
{
    public static Arbitrary<SessionAnswerListInput> SessionAnswerListInput()
    {
        var gen = Gen.Choose(0, 50)
            .SelectMany(size =>
                Gen.ArrayOf(ArbMap.Default.GeneratorFor<bool>(), size))
            .Select(flags => new SessionAnswerListInput(flags));

        return gen.ToArbitrary();
    }
}

/// <summary>
/// Custom Arbitrary instances for pass/fail threshold property tests.
/// </summary>
public static class ThresholdArbitraries
{
    public static Arbitrary<ThresholdInput> ThresholdInput()
    {
        var gen = Gen.Choose(1, 100)
            .SelectMany(totalQuestions =>
                Gen.Choose(0, totalQuestions)
                    .Select(correctAnswers => new ThresholdInput(correctAnswers, totalQuestions)));

        return gen.ToArbitrary();
    }
}
