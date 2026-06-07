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
/// Property 4: Answer Persistence Round-Trip
/// For any valid answer submission (with a valid sessionId, questionId, and selectedAnswerOptionId),
/// the stored session answer SHALL contain the exact same questionId and selectedAnswerOptionId that were submitted.
/// </summary>
[Trait("Feature", "quiz-app")]
[Trait("Property", "4: Answer Persistence Round-Trip")]
public class SessionPersistenceProperties
{
    private static QuizAppDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<QuizAppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new QuizAppDbContext(options);
    }

    /// <summary>
    /// Submitting an answer via the service persists the exact questionId and selectedAnswerOptionId
    /// that were submitted in the request.
    /// </summary>
    [Property(Arbitrary = new[] { typeof(AnswerSubmissionArbitraries) })]
    public async Task<bool> SubmitAnswer_PersistsExactQuestionIdAndSelectedAnswerOptionId(
        AnswerSubmissionInput input)
    {
        var dbName = $"SessionPersistence_{Guid.NewGuid()}";

        await using var setupContext = CreateDbContext(dbName);
        await setupContext.Database.EnsureCreatedAsync();

        // Create a quiz with the generated questions and answer options
        var quiz = new Quiz
        {
            TopicName = "Test Quiz",
            Description = "A quiz for persistence testing",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Questions = input.Questions.Select((q, idx) => new Question
            {
                Text = $"Question {idx + 1}",
                OrderIndex = idx,
                AnswerOptions = q.AnswerOptionIds.Select((optId, optIdx) => new AnswerOption
                {
                    Text = $"Option {optIdx + 1}",
                    IsCorrect = optIdx == 0 // First option is always correct
                }).ToList()
            }).ToList()
        };

        setupContext.Quizzes.Add(quiz);
        await setupContext.SaveChangesAsync();

        // Create a session
        var session = new QuizSession
        {
            Id = Guid.NewGuid(),
            QuizId = quiz.Id,
            UserId = null,
            StartedAt = DateTime.UtcNow,
            TotalQuestions = quiz.Questions.Count
        };

        setupContext.QuizSessions.Add(session);
        await setupContext.SaveChangesAsync();

        // Gather the actual persisted question IDs and their answer option IDs
        var questions = await setupContext.Questions
            .Include(q => q.AnswerOptions)
            .Where(q => q.QuizId == quiz.Id)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();

        // Pick the answer submissions based on the generated selection indices
        var submissions = new List<SubmitAnswerRequest>();
        for (int i = 0; i < questions.Count && i < input.Questions.Count; i++)
        {
            var question = questions[i];
            var selectionIndex = input.Questions[i].SelectedOptionIndex;
            var answerOptions = question.AnswerOptions.ToList();
            var selectedOption = answerOptions[selectionIndex % answerOptions.Count];

            submissions.Add(new SubmitAnswerRequest
            {
                QuestionId = question.Id,
                SelectedAnswerOptionId = selectedOption.Id
            });
        }

        // Submit answers via the service using a fresh context
        await using var serviceContext = CreateDbContext(dbName);
        var service = new QuizSessionService(serviceContext);

        foreach (var submission in submissions)
        {
            await service.SubmitAnswerAsync(session.Id, submission);
        }

        // Verify persistence using a fresh context
        await using var verifyContext = CreateDbContext(dbName);
        var storedAnswers = await verifyContext.SessionAnswers
            .Where(sa => sa.QuizSessionId == session.Id)
            .ToListAsync();

        // Verify count matches
        if (storedAnswers.Count != submissions.Count)
            return false;

        // Verify each stored answer has the exact questionId and selectedAnswerOptionId submitted
        foreach (var submission in submissions)
        {
            var storedAnswer = storedAnswers.FirstOrDefault(sa =>
                sa.QuestionId == submission.QuestionId);

            if (storedAnswer is null)
                return false;

            if (storedAnswer.SelectedAnswerOptionId != submission.SelectedAnswerOptionId)
                return false;
        }

        return true;
    }
}

/// <summary>
/// Input type representing a set of questions with answer selections for persistence testing.
/// </summary>
public record AnswerSubmissionInput(List<QuestionSubmission> Questions);

/// <summary>
/// Represents a question's answer options and which option index is selected.
/// </summary>
public record QuestionSubmission(List<int> AnswerOptionIds, int SelectedOptionIndex);

/// <summary>
/// Custom Arbitrary instances for answer persistence property tests.
/// </summary>
public static class AnswerSubmissionArbitraries
{
    public static Arbitrary<AnswerSubmissionInput> AnswerSubmissionInput()
    {
        // Each question has exactly 4 answer options; select one at random
        var selectedOptionIndexGen = Gen.Choose(0, 3);

        var questionSubmissionGen = selectedOptionIndexGen
            .Select(selectedIdx => new QuestionSubmission(
                AnswerOptionIds: new List<int> { 1, 2, 3, 4 }, // Placeholder IDs; actual IDs come from DB
                SelectedOptionIndex: selectedIdx
            ));

        // Generate 1 to 5 questions per quiz
        var questionsGen = Gen.Choose(1, 5)
            .SelectMany(count => Gen.ListOf(questionSubmissionGen, count))
            .Select(questions => new AnswerSubmissionInput(questions.ToList()));

        return questionsGen.ToArbitrary();
    }
}
