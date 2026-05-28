using QuizApp.DTOs;

namespace QuizApp.Services;

public interface IAuthService
{
    Task<AuthResultDto> RegisterAsync(RegisterRequest request);
    Task<AuthResultDto> LoginAsync(LoginRequest request);
}
