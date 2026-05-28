using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuizApp.DTOs;
using QuizApp.Services;

namespace QuizApp.Controllers;

[ApiController]
[Route("api/quizzes")]
public class QuizController : ControllerBase
{
    private readonly IQuizService _quizService;

    public QuizController(IQuizService quizService)
    {
        _quizService = quizService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<QuizSummaryDto>>> GetAllQuizzes()
    {
        var quizzes = await _quizService.GetAllQuizzesAsync();
        return Ok(quizzes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<QuizDetailDto>> GetQuizById(int id)
    {
        var quiz = await _quizService.GetQuizByIdAsync(id);
        return Ok(quiz);
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<QuizDetailDto>> CreateQuiz([FromBody] CreateQuizRequest request)
    {
        var quiz = await _quizService.CreateQuizAsync(request);
        return CreatedAtAction(nameof(GetQuizById), new { id = quiz.Id }, quiz);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<ActionResult<QuizDetailDto>> UpdateQuiz(int id, [FromBody] UpdateQuizRequest request)
    {
        var quiz = await _quizService.UpdateQuizAsync(id, request);
        return Ok(quiz);
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteQuiz(int id)
    {
        await _quizService.DeleteQuizAsync(id);
        return NoContent();
    }
}
