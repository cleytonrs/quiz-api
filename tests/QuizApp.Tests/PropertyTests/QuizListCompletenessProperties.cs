using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.Tests.PropertyTests;

/// <summary>
/// Property 8: Quiz List Completeness
/// For any set of quizzes stored in the system, the quiz list endpoint SHALL return all of them,
/// and each entry SHALL contain the topic name and the correct question count.
///
/// **Validates: Requirements 5.1**
/// </summary>
[Trait("Feature", "quiz-app")]
[Trait("Property", "8: Quiz List Completeness")]
public class QuizListCompletenessProperties
{
    private static QuizAppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<QuizAppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new QuizAppDbContext(options);
    }

    /// <summary>
    /// **Validates: Requirements 5.1**
    ///
    /// The quiz list returns the same number of quizzes as stored in the database,
    /// each with the correct topic name and question count.
    /// </summary>
    [Property(Arbitrary = new[] { typeof(QuizListArbitraries) })]
    public async Task<bool> GetAllQuizzes_ReturnsAllStoredQuizzes_WithCorrectTopicNamesAndQuestionCounts(
        QuizListInput input)
    {
        var dbName = $"QuizListCompleteness_{Guid.NewGuid()}";

        await using var context = CreateDbContext(dbName);
        await context.Database.EnsureCreatedAsync();

        // Store quizzes in the database
        foreach (var quizData in input.Quizzes)
        {
            var quiz = new Quiz
            {
                TopicName = quizData.TopicName,
                Description = quizData.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Questions = Enumerable.Range(0, quizData.QuestionCount)
                    .Select(i => new Question
                    {
                        Text = $"Question {i + 1}",
                        OrderIndex = i,
                        AnswerOptions = Enumerable.Range(0, 4)
                            .Select(j => new AnswerOption
                            {
                                Text = $"Option {j + 1}",
                                IsCorrect = j == 0
                            }).ToList()
                    }).ToList()
            };

            context.Quizzes.Add(quiz);
        }

        await context.SaveChangesAsync();

        // Use a fresh context for the service to avoid caching
        await using var queryContext = CreateDbContext(dbName);
        var service = new QuizService(queryContext);

        // Act
        var result = (await service.GetAllQuizzesAsync()).ToList();

        // Assert: count matches
        if (result.Count != input.Quizzes.Count)
            return false;

        // Assert: each quiz's topic name and question count match
        // Sort both by topic name for comparison since DB order may vary
        var storedQuizzes = input.Quizzes.OrderBy(q => q.TopicName).ThenBy(q => q.QuestionCount).ToList();
        var returnedQuizzes = result.OrderBy(q => q.TopicName).ThenBy(q => q.QuestionCount).ToList();

        for (int i = 0; i < storedQuizzes.Count; i++)
        {
            if (returnedQuizzes[i].TopicName != storedQuizzes[i].TopicName)
                return false;

            if (returnedQuizzes[i].QuestionCount != storedQuizzes[i].QuestionCount)
                return false;
        }

        return true;
    }
}

/// <summary>
/// Input type representing a list of quizzes to store.
/// </summary>
public record QuizListInput(List<QuizData> Quizzes);

/// <summary>
/// Data for a single quiz to be stored.
/// </summary>
public record QuizData(string TopicName, string Description, int QuestionCount);

/// <summary>
/// Custom Arbitrary instances for quiz list completeness property tests.
/// </summary>
public static class QuizListArbitraries
{
    public static Arbitrary<QuizListInput> QuizListInput()
    {
        var topicNameGen = ArbMap.Default.GeneratorFor<NonEmptyString>()
            .Select(s => s.Get);

        var descriptionGen = ArbMap.Default.GeneratorFor<NonEmptyString>()
            .Select(s => s.Get);

        var questionCountGen = Gen.Choose(1, 10);

        var quizDataGen = topicNameGen
            .SelectMany(topic => descriptionGen, (topic, desc) => new { topic, desc })
            .SelectMany(_ => questionCountGen, (pair, count) => new QuizData(pair.topic, pair.desc, count));

        var quizListGen = Gen.Choose(1, 5)
            .SelectMany(size => Gen.ListOf(quizDataGen, size))
            .Select(quizzes => new QuizListInput(quizzes.ToList()));

        return quizListGen.ToArbitrary();
    }
}
