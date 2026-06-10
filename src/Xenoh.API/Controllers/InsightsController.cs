using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xenoh.API.Auth;
using Xenoh.Application.Features.Insights.Commands.CoachChat;
using Xenoh.Application.Features.Insights.Queries.GetPlanProgressInsight;
using Xenoh.Application.Features.Insights.Queries.GetTrainingCoachTip;
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
    [Authorize(Policy = SubscriptionPolicies.RequirePro)]
    [EnableRateLimiting("ai")]
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

    /// <summary>
    /// Returns a plan-scoped AI progress insight focused on a single plan's week-over-week
    /// trajectory. Distinct from <see cref="GetMyAnalysis"/>, which is an account-wide review.
    /// </summary>
    [HttpGet("plan/{planId:guid}/progress")]
    [Authorize(Policy = SubscriptionPolicies.RequirePro)]
    [EnableRateLimiting("ai")]
    public async Task<IActionResult> GetPlanProgressInsight(Guid planId, [FromQuery] string? lang, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetPlanProgressInsightQuery(planId, lang), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Returns one personalized Xenoh Coach training tip for the current user.
    /// Cached server-side per (user, language) and re-generated when the
    /// underlying training snapshot or prompt version changes.
    /// </summary>
    [HttpGet("me/coach-tip")]
    [Authorize(Policy = SubscriptionPolicies.RequirePro)]
    [EnableRateLimiting("ai")]
    public async Task<IActionResult> GetMyCoachTip([FromQuery] string? lang, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetTrainingCoachTipQuery(lang), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Conversational AI coach. Stateless: the client sends the recent message
    /// history and receives a single text reply grounded in the user's training data.
    /// </summary>
    [HttpPost("me/coach-chat")]
    [Authorize(Policy = SubscriptionPolicies.RequirePro)]
    [EnableRateLimiting("ai")]
    public async Task<IActionResult> CoachChat([FromBody] CoachChatCommand command, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(command, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
