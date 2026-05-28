using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.DTOs;
using QuizApp.Exceptions;
using QuizApp.Models;

namespace QuizApp.Services;

public class QuizSessionService : IQuizSessionService
{
    private readonly QuizAppDbContext _context;

    public QuizSessionService(QuizAppDbContext context)
    {
        _context = context;
    }

    public async Task<QuizSessionDto> StartSessionAsync(int quizId, string? userId)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz is null)
        {
            throw new NotFoundException("Quiz not found");
        }

        var session = new QuizSession
        {
            Id = Guid.NewGuid(),
            QuizId = quizId,
            UserId = userId,
            StartedAt = DateTime.UtcNow,
            TotalQuestions = quiz.Questions.Count
        };

        _context.QuizSessions.Add(session);
        await _context.SaveChangesAsync();

        return new QuizSessionDto
        {
            Id = session.Id,
            QuizId = session.QuizId,
            StartedAt = session.StartedAt
        };
    }

    public async Task<AnswerResultDto> SubmitAnswerAsync(Guid sessionId, SubmitAnswerRequest request)
    {
        var session = await _context.QuizSessions
            .Include(s => s.Answers)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session is null)
        {
            throw new NotFoundException("Session not found");
        }

        if (session.CompletedAt is not null)
        {
            throw new ConflictException("Session already completed");
        }

        if (session.Answers.Any(a => a.QuestionId == request.QuestionId))
        {
            throw new ConflictException("Question already answered");
        }

        var correctOption = await _context.AnswerOptions
            .Where(a => a.QuestionId == request.QuestionId && a.IsCorrect)
            .FirstOrDefaultAsync();

        if (correctOption is null)
        {
            throw new NotFoundException("Question not found");
        }

        var isCorrect = request.SelectedAnswerOptionId == correctOption.Id;

        var sessionAnswer = new SessionAnswer
        {
            QuizSessionId = sessionId,
            QuestionId = request.QuestionId,
            SelectedAnswerOptionId = request.SelectedAnswerOptionId,
            IsCorrect = isCorrect
        };

        _context.SessionAnswers.Add(sessionAnswer);
        await _context.SaveChangesAsync();

        return new AnswerResultDto
        {
            IsCorrect = isCorrect,
            CorrectAnswerOptionId = correctOption.Id
        };
    }

    public async Task<QuizResultDto> CompleteSessionAsync(Guid sessionId)
    {
        var session = await _context.QuizSessions
            .Include(s => s.Answers)
            .Include(s => s.Quiz)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session is null)
        {
            throw new NotFoundException("Session not found");
        }

        if (session.CompletedAt is not null)
        {
            throw new ConflictException("Session already completed");
        }

        session.CompletedAt = DateTime.UtcNow;
        session.CorrectAnswerCount = session.Answers.Count(a => a.IsCorrect);
        session.Passed = session.TotalQuestions > 0
            && (double)session.CorrectAnswerCount / session.TotalQuestions >= 0.70;

        await _context.SaveChangesAsync();

        var elapsedTimeSeconds = (session.CompletedAt.Value - session.StartedAt).TotalSeconds;

        return new QuizResultDto
        {
            SessionId = session.Id,
            QuizTopicName = session.Quiz.TopicName,
            TotalQuestions = session.TotalQuestions,
            CorrectAnswers = session.CorrectAnswerCount,
            ScorePercentage = session.TotalQuestions > 0
                ? Math.Round((double)session.CorrectAnswerCount / session.TotalQuestions * 100, 1)
                : 0,
            Passed = session.Passed,
            ElapsedTimeSeconds = elapsedTimeSeconds,
            CompletedAt = session.CompletedAt.Value
        };
    }

    public async Task<QuizResultDto> GetSessionResultAsync(Guid sessionId)
    {
        var session = await _context.QuizSessions
            .Include(s => s.Quiz)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session is null)
        {
            throw new NotFoundException("Session not found");
        }

        if (session.CompletedAt is null)
        {
            throw new ConflictException("Session not yet completed");
        }

        var elapsedTimeSeconds = (session.CompletedAt.Value - session.StartedAt).TotalSeconds;

        return new QuizResultDto
        {
            SessionId = session.Id,
            QuizTopicName = session.Quiz.TopicName,
            TotalQuestions = session.TotalQuestions,
            CorrectAnswers = session.CorrectAnswerCount,
            ScorePercentage = session.TotalQuestions > 0
                ? Math.Round((double)session.CorrectAnswerCount / session.TotalQuestions * 100, 1)
                : 0,
            Passed = session.Passed,
            ElapsedTimeSeconds = elapsedTimeSeconds,
            CompletedAt = session.CompletedAt.Value
        };
    }
}
