using Microsoft.EntityFrameworkCore;
using QuizApp.Data;
using QuizApp.DTOs;

namespace QuizApp.Services;

public class DashboardService : IDashboardService
{
    private readonly QuizAppDbContext _dbContext;

    public DashboardService(QuizAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardDto> GetDashboardAsync(string userId)
    {
        var sessions = await _dbContext.QuizSessions
            .Include(s => s.Quiz)
            .Where(s => s.UserId == userId && s.CompletedAt != null)
            .OrderByDescending(s => s.CompletedAt)
            .Select(s => new DashboardSessionDto
            {
                SessionId = s.Id,
                QuizTopicName = s.Quiz.TopicName,
                Score = s.CorrectAnswerCount,
                TotalQuestions = s.TotalQuestions,
                Passed = s.Passed,
                CompletedAt = s.CompletedAt!.Value
            })
            .ToListAsync();

        return new DashboardDto { Sessions = sessions };
    }
}
