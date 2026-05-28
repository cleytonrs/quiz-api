using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.DTOs;
using QuizApp.Exceptions;
using QuizApp.Models;

namespace QuizApp.Services;

public class QuizService : IQuizService
{
    private readonly QuizAppDbContext _context;

    public QuizService(QuizAppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<QuizSummaryDto>> GetAllQuizzesAsync()
    {
        return await _context.Quizzes
            .Select(q => new QuizSummaryDto
            {
                Id = q.Id,
                TopicName = q.TopicName,
                Description = q.Description,
                QuestionCount = q.Questions.Count
            })
            .ToListAsync();
    }

    public async Task<QuizDetailDto> GetQuizByIdAsync(int quizId)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions.OrderBy(question => question.OrderIndex))
                .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz is null)
        {
            throw new NotFoundException("Quiz not found");
        }

        return MapToDetailDto(quiz);
    }

    public async Task<QuizDetailDto> CreateQuizAsync(CreateQuizRequest request)
    {
        var quiz = new Quiz
        {
            TopicName = request.TopicName,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Questions = request.Questions.Select((q, index) => new Question
            {
                Text = q.Text,
                OrderIndex = index,
                AnswerOptions = q.AnswerOptions.Select(o => new AnswerOption
                {
                    Text = o.Text,
                    IsCorrect = o.IsCorrect
                }).ToList()
            }).ToList()
        };

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        return MapToDetailDto(quiz);
    }

    public async Task<QuizDetailDto> UpdateQuizAsync(int quizId, UpdateQuizRequest request)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(q => q.AnswerOptions)
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz is null)
        {
            throw new NotFoundException("Quiz not found");
        }

        // Update quiz properties
        quiz.TopicName = request.TopicName;
        quiz.Description = request.Description;
        quiz.UpdatedAt = DateTime.UtcNow;

        // Remove session answers that reference the old answer options/questions
        var questionIds = quiz.Questions.Select(q => q.Id).ToList();
        var relatedSessionAnswers = await _context.SessionAnswers
            .Where(sa => questionIds.Contains(sa.QuestionId))
            .ToListAsync();
        _context.SessionAnswers.RemoveRange(relatedSessionAnswers);

        // Remove existing questions (cascade will remove answer options)
        _context.Questions.RemoveRange(quiz.Questions);

        // Add new questions
        quiz.Questions = request.Questions.Select((q, index) => new Question
        {
            Text = q.Text,
            OrderIndex = index,
            AnswerOptions = q.AnswerOptions.Select(o => new AnswerOption
            {
                Text = o.Text,
                IsCorrect = o.IsCorrect
            }).ToList()
        }).ToList();

        await _context.SaveChangesAsync();

        return MapToDetailDto(quiz);
    }

    public async Task DeleteQuizAsync(int quizId)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == quizId);

        if (quiz is null)
        {
            throw new NotFoundException("Quiz not found");
        }

        // Remove session answers that reference this quiz's questions
        var questionIds = quiz.Questions.Select(q => q.Id).ToList();
        var relatedSessionAnswers = await _context.SessionAnswers
            .Where(sa => questionIds.Contains(sa.QuestionId))
            .ToListAsync();
        _context.SessionAnswers.RemoveRange(relatedSessionAnswers);

        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();
    }

    private static QuizDetailDto MapToDetailDto(Quiz quiz)
    {
        return new QuizDetailDto
        {
            Id = quiz.Id,
            TopicName = quiz.TopicName,
            Description = quiz.Description,
            Questions = quiz.Questions.OrderBy(q => q.OrderIndex).Select(q => new QuestionDto
            {
                Id = q.Id,
                Text = q.Text,
                OrderIndex = q.OrderIndex,
                AnswerOptions = q.AnswerOptions.Select(o => new AnswerOptionDto
                {
                    Id = o.Id,
                    Text = o.Text,
                    IsCorrect = o.IsCorrect
                }).ToList()
            }).ToList()
        };
    }
}
