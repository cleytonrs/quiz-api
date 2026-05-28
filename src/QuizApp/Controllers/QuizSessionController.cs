using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using QuizApp.DTOs;
using QuizApp.Services;

namespace QuizApp.Controllers;

[ApiController]
[Route("api")]
public class QuizSessionController : ControllerBase
{
    private readonly IQuizSessionService _quizSessionService;

    public QuizSessionController(IQuizSessionService quizSessionService)
    {
        _quizSessionService = quizSessionService;
    }

    [HttpPost("quizzes/{quizId}/sessions")]
    public async Task<ActionResult<QuizSessionDto>> StartSession(int quizId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value;

        var session = await _quizSessionService.StartSessionAsync(quizId, userId);
        return CreatedAtAction(nameof(GetSessionResult), new { sessionId = session.Id }, session);
    }

    [HttpPost("sessions/{sessionId}/answers")]
    public async Task<ActionResult<AnswerResultDto>> SubmitAnswer(Guid sessionId, [FromBody] SubmitAnswerRequest request)
    {
        var result = await _quizSessionService.SubmitAnswerAsync(sessionId, request);
        return Ok(result);
    }

    [HttpPost("sessions/{sessionId}/complete")]
    public async Task<ActionResult<QuizResultDto>> CompleteSession(Guid sessionId)
    {
        var result = await _quizSessionService.CompleteSessionAsync(sessionId);
        return Ok(result);
    }

    [HttpGet("sessions/{sessionId}/result")]
    public async Task<ActionResult<QuizResultDto>> GetSessionResult(Guid sessionId)
    {
        var result = await _quizSessionService.GetSessionResultAsync(sessionId);
        return Ok(result);
    }
}
