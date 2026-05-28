using QuizApp.DTOs;

namespace QuizApp.Services;

public interface IQuizService
{
    Task<IEnumerable<QuizSummaryDto>> GetAllQuizzesAsync();
    Task<QuizDetailDto> GetQuizByIdAsync(int quizId);
    Task<QuizDetailDto> CreateQuizAsync(CreateQuizRequest request);
    Task<QuizDetailDto> UpdateQuizAsync(int quizId, UpdateQuizRequest request);
    Task DeleteQuizAsync(int quizId);
}
