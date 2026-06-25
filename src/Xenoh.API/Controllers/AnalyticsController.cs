using System.Security.Claims;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.WebsiteAnalytics;
using Xenoh.Domain.Enums;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/analytics")]
public sealed class AnalyticsController(IMediator mediator) : ControllerBase
{
    [HttpPost("page-view")]
    [AllowAnonymous]
    public async Task<IActionResult> TrackPageView([FromBody] TrackWebsiteActivityRequest request, CancellationToken ct)
    {
        await mediator.Send(ToCommand(request, WebsiteActivityEventType.PageView, GetOptionalUserId()), ct);
        return NoContent();
    }

    [HttpPost("usage")]
    [Authorize]
    public async Task<IActionResult> TrackUsage([FromBody] TrackWebsiteActivityRequest request, CancellationToken ct)
    {
        try
        {
            await mediator.Send(ToCommand(request, WebsiteActivityEventType.SessionUsage, GetOptionalUserId()), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private TrackWebsiteActivityCommand ToCommand(
        TrackWebsiteActivityRequest request,
        WebsiteActivityEventType eventType,
        Guid? userId) =>
        new(
            eventType,
            request.SessionId,
            request.Path,
            request.PreviousPath,
            request.Referrer,
            request.UtmSource,
            request.UtmMedium,
            request.UtmCampaign,
            request.DurationSeconds,
            Request.Headers.UserAgent.ToString(),
            userId);

    private Guid? GetOptionalUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

public sealed record TrackWebsiteActivityRequest(
    string SessionId,
    string Path,
    string? PreviousPath,
    string? Referrer,
    string? UtmSource,
    string? UtmMedium,
    string? UtmCampaign,
    int? DurationSeconds);
