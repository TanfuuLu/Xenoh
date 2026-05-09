using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Insights.Queries.GetUserAnalysis;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/insights")]
[Authorize]
public sealed class InsightsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Returns the AI-generated training analysis for the current user.
    /// Cached server-side per (user, language) and re-generated only when
    /// the underlying training/body data changes.
    /// </summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyAnalysis([FromQuery] string? lang, CancellationToken ct)
    {
        try
        {
            var language = string.Equals(lang, "vi", StringComparison.OrdinalIgnoreCase) ? "vi" : "en";
            var result = await mediator.Send(new GetUserAnalysisQuery(language), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
