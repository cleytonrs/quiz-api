using QuizApp.DTOs;

namespace QuizApp.Services;

public interface IDashboardService
{
    Task<DashboardDto> GetDashboardAsync(string userId);
}
