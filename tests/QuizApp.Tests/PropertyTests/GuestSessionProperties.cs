using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.DTOs;
using QuizApp.Models;
using QuizApp.Services;

namespace QuizApp.Tests.PropertyTests;

/// <summary>
/// Property 13: Guest Session Non-Association
/// For any quiz session completed without authentication, the session record SHALL have a null userId,
/// and the quiz result SHALL still be calculable and returnable to the client.
///
/// **Validates: Requirements 11.3**
/// </summary>
[Trait("Feature", "quiz-app")]
[Trait("Property", "13: Guest Session Non-Association")]
public class GuestSessionProperties
{
    private static QuizAppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<QuizAppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new QuizAppDbContext(options);
    }

    /// <summary>
    /// **Validates: Requirements 11.3**
    ///
    /// Starting a session with null userId (guest) and completing it should:
    /// 1. Store the session with null UserId
    /// 2. Still produce a valid QuizResultDto with correct data
    /// </summary>
    [Property(Arbitrary = new[] { typeof(GuestSessionArbitraries) })]
    public async Task<bool> GuestSession_HasNullUserId_AndResultIsCalculable(
        GuestSessionInput input)
    {
        var dbName = $"GuestSession_{Guid.NewGuid()}";

        await using var setupContext = CreateDbContext(dbName);
        await setupContext.Database.EnsureCreatedAsync();

        // Create a quiz with the generated number of questions
        var quiz = new Quiz
        {
            TopicName = input.TopicName,
            Description = "Guest session test quiz",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Questions = Enumerable.Range(0, input.QuestionCount).Select(idx => new Question
            {
                Text = $"Question {idx + 1}",
                OrderIndex = idx,
                AnswerOptions = Enumerable.Range(0, 4).Select(optIdx => new AnswerOption
                {
                    Text = $"Option {optIdx + 1}",
                    IsCorrect = optIdx == 0 // First option is always correct
                }).ToList()
            }).ToList()
        };

        setupContext.Quizzes.Add(quiz);
        await setupContext.SaveChangesAsync();

        // Start a session as a guest (userId = null) via the service
        await using var serviceContext = CreateDbContext(dbName);
        var service = new QuizSessionService(serviceContext);

        var sessionDto = await service.StartSessionAsync(quiz.Id, null);

        // Submit answers based on the generated answer selections
        var questions = await serviceContext.Questions
            .Include(q => q.AnswerOptions)
            .Where(q => q.QuizId == quiz.Id)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();

        for (int i = 0; i < questions.Count; i++)
        {
            var question = questions[i];
            var answerOptions = question.AnswerOptions.ToList();
            var selectedIndex = input.AnswerSelections[i] % answerOptions.Count;
            var selectedOption = answerOptions[selectedIndex];

            await service.SubmitAnswerAsync(sessionDto.Id, new SubmitAnswerRequest
            {
                QuestionId = question.Id,
                SelectedAnswerOptionId = selectedOption.Id
            });
        }

        // Complete the session
        var result = await service.CompleteSessionAsync(sessionDto.Id);

        // Verify: Check the stored session has null UserId
        await using var verifyContext = CreateDbContext(dbName);
        var storedSession = await verifyContext.QuizSessions
            .FirstOrDefaultAsync(s => s.Id == sessionDto.Id);

        if (storedSession is null)
            return false;

        // Property assertion 1: UserId must be null for guest sessions
        if (storedSession.UserId is not null)
            return false;

        // Property assertion 2: Result is calculable - QuizResultDto has valid data
        if (result.SessionId != sessionDto.Id)
            return false;

        if (result.QuizTopicName != input.TopicName)
            return false;

        if (result.TotalQuestions != input.QuestionCount)
            return false;

        if (result.CorrectAnswers < 0 || result.CorrectAnswers > input.QuestionCount)
            return false;

        if (result.ScorePercentage < 0 || result.ScorePercentage > 100)
            return false;

        if (result.ElapsedTimeSeconds < 0)
            return false;

        if (result.CompletedAt == default)
            return false;

        return true;
    }
}

/// <summary>
/// Input type for guest session property tests.
/// </summary>
public record GuestSessionInput(
    string TopicName,
    int QuestionCount,
    List<int> AnswerSelections);

/// <summary>
/// Custom Arbitrary instances for guest session property tests.
/// </summary>
public static class GuestSessionArbitraries
{
    public static Arbitrary<GuestSessionInput> GuestSessionInput()
    {
        var topicNameGen = Gen.Elements(
            "HTML", "CSS", "JavaScript", "Python", "PHP", "TypeScript", "CSharp", "Java");

        var questionCountGen = Gen.Choose(1, 5);

        var inputGen = questionCountGen.SelectMany<int, GuestSessionInput>(count =>
        {
            // Generate answer selections (0-3) for each question
            var selectionsGen = Gen.ListOf<int>(Gen.Choose(0, 3), count)
                .Select(selections => selections.ToList());

            return topicNameGen.SelectMany<string, GuestSessionInput>(topic =>
                selectionsGen.Select(selections =>
                    new GuestSessionInput(topic, count, selections)));
        });

        return inputGen.ToArbitrary();
    }
}
