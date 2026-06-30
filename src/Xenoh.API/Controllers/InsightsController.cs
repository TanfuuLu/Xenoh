using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xenoh.API.Security;
using Xenoh.API.Auth;
using Xenoh.Application.Features.Insights.Commands.AiChat;
using Xenoh.Application.Features.Insights.Commands.CoachChat;
using Xenoh.Application.Features.Insights.Queries.AiChat;
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
    [EnableRateLimiting(RateLimitPolicyNames.Ai)]
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
    [EnableRateLimiting(RateLimitPolicyNames.Ai)]
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
    [EnableRateLimiting(RateLimitPolicyNames.Ai)]
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

    [HttpGet("me/coach-chat/conversations")]
    [Authorize(Policy = SubscriptionPolicies.RequirePro)]
    public async Task<IActionResult> ListCoachChatConversations(
        [FromQuery] DateTime? cursor,
        [FromQuery] int take = 20,
        CancellationToken ct = default)
    {
        try
        {
            var result = await mediator.Send(new ListAiChatConversationsQuery(cursor, take), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("me/coach-chat/conversations")]
    [Authorize(Policy = SubscriptionPolicies.RequirePro)]
    public async Task<IActionResult> CreateCoachChatConversation(
        [FromBody] CreateCoachChatConversationRequest? request,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new CreateAiChatConversationCommand(request?.Title), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("me/coach-chat/conversations/{conversationId:guid}")]
    [Authorize(Policy = SubscriptionPolicies.RequirePro)]
    public async Task<IActionResult> UpdateCoachChatConversation(
        Guid conversationId,
        [FromBody] UpdateCoachChatConversationRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(
                new UpdateAiChatConversationCommand(conversationId, request.Title, request.IsArchived),
                ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("me/coach-chat/conversations/{conversationId:guid}/messages")]
    [Authorize(Policy = SubscriptionPolicies.RequirePro)]
    public async Task<IActionResult> GetCoachChatMessages(
        Guid conversationId,
        [FromQuery] DateTime? before,
        [FromQuery] int take = 30,
        CancellationToken ct = default)
    {
        try
        {
            var result = await mediator.Send(new GetAiChatMessagesQuery(conversationId, before, take), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("me/coach-chat/conversations/{conversationId:guid}/messages")]
    [Authorize(Policy = SubscriptionPolicies.RequirePro)]
    [EnableRateLimiting(RateLimitPolicyNames.Ai)]
    public async Task<IActionResult> SendCoachChatMessage(
        Guid conversationId,
        [FromBody] SendCoachChatMessageRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(
                new SendAiChatMessageCommand(conversationId, request.Content, request.Language),
                ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public sealed record CreateCoachChatConversationRequest(string? Title);

public sealed record UpdateCoachChatConversationRequest(string? Title, bool? IsArchived);

public sealed record SendCoachChatMessageRequest(string Content, string? Language);
