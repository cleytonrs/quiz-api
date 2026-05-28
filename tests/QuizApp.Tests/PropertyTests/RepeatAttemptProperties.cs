using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.Tests.PropertyTests;

/// <summary>
/// Property 12: Repeat Attempt Preservation
/// For any user who completes a quiz K times (K ≥ 1), the system SHALL store exactly K separate
/// session records, and the dashboard SHALL display all K attempts for that quiz.
///
/// **Validates: Requirements 9.2, 9.3**
/// </summary>
[Trait("Feature", "quiz-app")]
[Trait("Property", "12: Repeat Attempt Preservation")]
public class RepeatAttemptProperties
{
    private static QuizAppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<QuizAppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new QuizAppDbContext(options);
    }

    /// <summary>
    /// **Validates: Requirements 9.2, 9.3**
    ///
    /// For any user who completes a quiz K times (K randomly generated between 1 and 10),
    /// GetDashboardAsync returns exactly K sessions for that quiz.
    /// All repeat attempts are preserved and visible on the dashboard.
    /// </summary>
    [Property(Arbitrary = new[] { typeof(RepeatAttemptArbitraries) })]
    public async Task<bool> Dashboard_PreservesAllRepeatAttempts(RepeatAttemptInput input)
    {
        var dbName = $"RepeatAttempt_{Guid.NewGuid()}";

        await using var setupContext = CreateDbContext(dbName);
        await setupContext.Database.EnsureCreatedAsync();

        // Create a quiz
        var quiz = new Quiz
        {
            TopicName = input.QuizTopicName,
            Description = "Quiz for repeat attempt property test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Questions = new List<Question>
            {
                new Question
                {
                    Text = "Sample question",
                    OrderIndex = 0,
                    AnswerOptions = Enumerable.Range(0, 4).Select(i => new AnswerOption
                    {
                        Text = $"Option {i + 1}",
                        IsCorrect = i == 0
                    }).ToList()
                }
            }
        };

        setupContext.Quizzes.Add(quiz);
        await setupContext.SaveChangesAsync();

        // Create K completed sessions for the same user and same quiz
        var userId = input.UserId;
        var baseDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (int i = 0; i < input.AttemptCount; i++)
        {
            var completedAt = baseDate.AddHours(i + 1);
            var session = new QuizSession
            {
                Id = Guid.NewGuid(),
                QuizId = quiz.Id,
                UserId = userId,
                StartedAt = completedAt.AddMinutes(-5),
                CompletedAt = completedAt,
                CorrectAnswerCount = 1,
                TotalQuestions = 1,
                Passed = true
            };

            setupContext.QuizSessions.Add(session);
        }

        await setupContext.SaveChangesAsync();

        // Query via DashboardService using a fresh context
        await using var queryContext = CreateDbContext(dbName);
        var service = new DashboardService(queryContext);

        var dashboard = await service.GetDashboardAsync(userId);

        // Verify: exactly K sessions are returned (all repeat attempts preserved)
        return dashboard.Sessions.Count == input.AttemptCount;
    }
}

/// <summary>
/// Input type for repeat attempt preservation property tests.
/// </summary>
public record RepeatAttemptInput(string UserId, string QuizTopicName, int AttemptCount);

/// <summary>
/// Custom Arbitrary instances for repeat attempt preservation property tests.
/// </summary>
public static class RepeatAttemptArbitraries
{
    public static Arbitrary<RepeatAttemptInput> RepeatAttemptInput()
    {
        var userIdGen = Gen.Elements(
            "user-1", "user-2", "user-3", "user-4", "user-5");

        var topicNameGen = Gen.Elements(
            "JavaScript", "Python", "HTML", "CSS", "PHP", "TypeScript");

        // K between 1 and 10
        var attemptCountGen = Gen.Choose(1, 10);

        var inputGen = userIdGen.SelectMany(userId =>
            topicNameGen.SelectMany(topicName =>
                attemptCountGen.Select(attemptCount =>
                    new RepeatAttemptInput(userId, topicName, attemptCount))));

        return inputGen.ToArbitrary();
    }
}
