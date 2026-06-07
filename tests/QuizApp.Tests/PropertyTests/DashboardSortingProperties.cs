using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.Tests.PropertyTests;

/// <summary>
/// Property 11: Dashboard Chronological Ordering
/// For any set of completed quiz sessions belonging to a user, the dashboard response SHALL return them
/// sorted by completion date in descending order (most recent first).
/// </summary>
[Trait("Feature", "quiz-app")]
[Trait("Property", "11: Dashboard Chronological Ordering")]
public class DashboardSortingProperties
{
    private static QuizAppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<QuizAppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new QuizAppDbContext(options);
    }

    /// <summary>
    /// For any set of completed sessions with random completion dates,
    /// GetDashboardAsync returns them sorted by CompletedAt descending (most recent first).
    /// </summary>
    [Property(Arbitrary = new[] { typeof(DashboardSortingArbitraries) })]
    public async Task<bool> Dashboard_SessionsAreSortedByCompletedAtDescending(
        DashboardSortingInput input)
    {
        var dbName = $"DashboardSorting_{Guid.NewGuid()}";

        await using var setupContext = CreateDbContext(dbName);
        await setupContext.Database.EnsureCreatedAsync();

        // Create a quiz
        var quiz = new Quiz
        {
            TopicName = "Sorting Test Quiz",
            Description = "Quiz for dashboard sorting property test",
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

        // Create multiple completed sessions with the generated completion dates
        var userId = input.UserId;
        foreach (var completedAt in input.CompletionDates)
        {
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

        // Verify: sessions count matches
        if (dashboard.Sessions.Count != input.CompletionDates.Count)
            return false;

        // Verify: sessions are sorted by CompletedAt descending
        for (int i = 0; i < dashboard.Sessions.Count - 1; i++)
        {
            if (dashboard.Sessions[i].CompletedAt < dashboard.Sessions[i + 1].CompletedAt)
                return false;
        }

        return true;
    }
}

/// <summary>
/// Input type for dashboard sorting property tests.
/// </summary>
public record DashboardSortingInput(string UserId, List<DateTime> CompletionDates);

/// <summary>
/// Custom Arbitrary instances for dashboard sorting property tests.
/// </summary>
public static class DashboardSortingArbitraries
{
    public static Arbitrary<DashboardSortingInput> DashboardSortingInput()
    {
        var userIdGen = Gen.Elements(
            "user-1", "user-2", "user-3", "user-4", "user-5");

        // Generate random completion dates within a reasonable range
        var baseDateGen = Gen.Constant(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var offsetMinutesGen = Gen.Choose(0, 525600); // up to ~1 year in minutes

        var dateGen = offsetMinutesGen.Select(minutes =>
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(minutes));

        var sessionCountGen = Gen.Choose(2, 8);

        var inputGen = sessionCountGen.SelectMany<int, DashboardSortingInput>(count =>
        {
            var datesGen = Gen.ListOf(dateGen, count)
                .Select(dates => dates.ToList());

            return userIdGen.SelectMany<string, DashboardSortingInput>(userId =>
                datesGen.Select(dates =>
                    new DashboardSortingInput(userId, dates)));
        });

        return inputGen.ToArbitrary();
    }
}
