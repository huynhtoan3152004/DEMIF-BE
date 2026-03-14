using Demif.Application.Features.Lessons.CheckShadowing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Demif.Api.Controllers;

/// <summary>
/// API Controller dành riêng cho các tác vụ Shadowing.
/// Sử dụng chung route "api/lessons" với LessonsController để giữ nguyên tính tương thích frontend.
/// </summary>
[Route("api/lessons")]
[ApiController]
public class ShadowingController : ControllerBase
{
    private readonly CheckShadowingService _checkShadowingService;

    public ShadowingController(CheckShadowingService checkShadowingService)
    {
        _checkShadowingService = checkShadowingService;
    }

    /// <summary>
    /// Shadowing check — text-fallback mode (browser Web Speech API).
    /// FE records audio → Web Speech API transcribes → sends UserText.
    /// Backend runs LCS diff vs transcript and returns word-level feedback.
    /// POST /api/lessons/{id}/segments/{segmentIndex}/shadowing
    /// </summary>
    [HttpPost("{id:guid}/segments/{segmentIndex:int}/shadowing")]
    [Authorize]
    public async Task<IActionResult> CheckShadowing(
        Guid id,
        int segmentIndex,
        [FromBody] CheckShadowingRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserIdOrNull()!.Value;
        var result = await _checkShadowingService.ExecuteAsync(id, segmentIndex, request, userId, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "NotFound"   => NotFound(new { error = result.Error.Message }),
                "Validation" => BadRequest(new { error = result.Error.Message }),
                _            => BadRequest(new { error = result.Error.Message })
            };
        }

        return Ok(result.Value);
    }

    private Guid? GetUserIdOrNull()
    {
        var userIdClaim = User.FindFirst("userId")?.Value
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
