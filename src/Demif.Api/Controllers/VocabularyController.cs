using Demif.Api.Contracts;
using Demif.Application.Abstractions.Services;
using Demif.Application.Features.Me.Vocabulary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Demif.Api.Controllers;

[ApiController]
[Authorize]
[Route(ApiRoutes.Me.Vocabulary)]
[Produces("application/json")]
[Tags("Me")]
public class VocabularyController : ControllerBase
{
    private readonly VocabularyService _vocabularyService;
    private readonly ICurrentUserService _currentUserService;

    public VocabularyController(VocabularyService vocabularyService, ICurrentUserService currentUserService)
    {
        _vocabularyService = vocabularyService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetVocabulary([FromQuery] VocabularyQueryRequest request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
            return Unauthorized();

        var result = await _vocabularyService.GetAsync(userId, request, false, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetVocabularyOverview(CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
            return Unauthorized();

        var result = await _vocabularyService.GetOverviewAsync(userId, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetVocabularySuggestions([FromQuery] Guid lessonId, [FromQuery] VocabularySuggestionQuery request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
            return Unauthorized();

        var result = await _vocabularyService.GetSuggestionsAsync(userId, lessonId, request, cancellationToken);
        if (result.IsFailure)
            return result.Error.Code == "NotFound"
                ? NotFound(new { error = result.Error.Code, message = result.Error.Message })
                : BadRequest(new { error = result.Error.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    [HttpGet("due")]
    public async Task<IActionResult> GetDueVocabulary([FromQuery] VocabularyQueryRequest request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
            return Unauthorized();

        var result = await _vocabularyService.GetAsync(userId, request, true, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> SaveVocabulary([FromBody] SaveVocabularyRequest request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
            return Unauthorized();

        var result = await _vocabularyService.SaveAsync(userId, request, cancellationToken);
        if (result.IsFailure)
            return BadRequest(new { error = result.Error.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    [HttpPost("{id:guid}/review")]
    public async Task<IActionResult> ReviewVocabulary(Guid id, [FromBody] ReviewVocabularyRequest request, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
            return Unauthorized();

        var result = await _vocabularyService.ReviewAsync(userId, id, request, cancellationToken);
        if (result.IsFailure)
            return result.Error.Code == "NotFound"
                ? NotFound(new { error = result.Error.Code, message = result.Error.Message })
                : BadRequest(new { error = result.Error.Code, message = result.Error.Message });

        return Ok(result.Value);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteVocabulary(Guid id, CancellationToken cancellationToken)
    {
        if (_currentUserService.UserId is not { } userId)
            return Unauthorized();

        var result = await _vocabularyService.DeleteAsync(userId, id, cancellationToken);
        if (result.IsFailure)
            return result.Error.Code == "NotFound"
                ? NotFound(new { error = result.Error.Code, message = result.Error.Message })
                : BadRequest(new { error = result.Error.Code, message = result.Error.Message });

        return NoContent();
    }
}