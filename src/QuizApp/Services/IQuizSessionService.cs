using QuizApp.DTOs;

namespace QuizApp.Services;

public interface IQuizSessionService
{
    Task<QuizSessionDto> StartSessionAsync(int quizId, string? userId);
    Task<AnswerResultDto> SubmitAnswerAsync(Guid sessionId, SubmitAnswerRequest request);
    Task<QuizResultDto> CompleteSessionAsync(Guid sessionId);
    Task<QuizResultDto> GetSessionResultAsync(Guid sessionId);
}
