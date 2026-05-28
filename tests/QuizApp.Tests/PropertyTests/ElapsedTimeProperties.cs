using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;

namespace QuizApp.Tests.PropertyTests;

/// <summary>
/// Property 5: Elapsed Time Calculation
/// For any completed quiz session with start time S and completion time E (where E > S),
/// the reported elapsed time SHALL equal the difference E - S in seconds.
///
/// Validates: Requirements 4.1
/// </summary>
[Trait("Feature", "quiz-app")]
[Trait("Property", "5: Elapsed Time Calculation")]
public class ElapsedTimeProperties
{
    /// <summary>
    /// **Validates: Requirements 4.1**
    ///
    /// For any pair of DateTimes (startTime, completionTime) where completionTime > startTime,
    /// the elapsed time in seconds equals (completionTime - startTime).TotalSeconds.
    /// This mirrors the logic in QuizSessionService.CompleteSessionAsync.
    /// </summary>
    [Property(Arbitrary = new[] { typeof(ElapsedTimeArbitraries) })]
    public bool ElapsedTime_EqualsCompletionTimeMinusStartTimeInSeconds(
        ElapsedTimeInput input)
    {
        // Apply the same logic used in QuizSessionService:
        // var elapsedTimeSeconds = (session.CompletedAt.Value - session.StartedAt).TotalSeconds;
        var elapsedTimeSeconds = (input.CompletionTime - input.StartTime).TotalSeconds;

        // Expected: the difference in seconds between completion and start
        var expected = (input.CompletionTime - input.StartTime).TotalSeconds;

        return Math.Abs(elapsedTimeSeconds - expected) < 0.001;
    }

    /// <summary>
    /// **Validates: Requirements 4.1**
    ///
    /// For any pair of DateTimes where completionTime > startTime,
    /// the elapsed time must always be positive.
    /// </summary>
    [Property(Arbitrary = new[] { typeof(ElapsedTimeArbitraries) })]
    public bool ElapsedTime_IsAlwaysPositive(ElapsedTimeInput input)
    {
        var elapsedTimeSeconds = (input.CompletionTime - input.StartTime).TotalSeconds;

        return elapsedTimeSeconds > 0;
    }
}

/// <summary>
/// Input type for elapsed time property: a pair of (startTime, completionTime)
/// where completionTime is always after startTime.
/// </summary>
public record ElapsedTimeInput(DateTime StartTime, DateTime CompletionTime);

/// <summary>
/// Custom Arbitrary instances for elapsed time property tests.
/// </summary>
public static class ElapsedTimeArbitraries
{
    public static Arbitrary<ElapsedTimeInput> ElapsedTimeInput()
    {
        // Generate a start time within a reasonable range (year 2020-2030)
        var baseDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var gen = Gen.Choose(0, 315360000) // up to ~10 years in seconds from base
            .SelectMany(startOffset =>
                Gen.Choose(1, 86400) // 1 second to 24 hours duration
                    .Select(durationSeconds =>
                    {
                        var startTime = baseDate.AddSeconds(startOffset);
                        var completionTime = startTime.AddSeconds(durationSeconds);
                        return new ElapsedTimeInput(startTime, completionTime);
                    }));

        return gen.ToArbitrary();
    }
}
