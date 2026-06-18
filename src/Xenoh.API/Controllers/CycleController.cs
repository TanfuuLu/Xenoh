using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xenoh.API.Security;
using Xenoh.API.Auth;
using Xenoh.Application.Features.Cycle.Commands.DeleteCycleDailyLog;
using Xenoh.Application.Features.Cycle.Commands.UpdateCycleSettings;
using Xenoh.Application.Features.Cycle.Commands.UpsertCycleDailyLog;
using Xenoh.Application.Features.Cycle.Queries.GetClientCycleOverview;
using Xenoh.Application.Features.Cycle.Queries.GetCycleDayMarkers;
using Xenoh.Application.Features.Cycle.Queries.GetCycleInsight;
using Xenoh.Application.Features.Cycle.Queries.GetCycleLogs;
using Xenoh.Application.Features.Cycle.Queries.GetCycleOverview;
using Xenoh.Application.Features.Cycle.Queries.GetCycleSettings;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/cycle")]
[Authorize]
public sealed class CycleController(IMediator mediator) : ControllerBase
{
    [HttpPut("logs/{date}")]
    public async Task<IActionResult> UpsertLog(
        DateOnly date, [FromBody] UpsertCycleDailyLogCommand command, CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(command with { Date = date }, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("logs/{date}")]
    public async Task<IActionResult> DeleteLog(DateOnly date, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new DeleteCycleDailyLogCommand(date), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(new GetCycleLogsQuery(from, to), ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(new GetCycleOverviewQuery(), ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings(CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(new GetCycleSettingsQuery(), ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] UpdateCycleSettingsCommand command, CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(command, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("day-markers")]
    public async Task<IActionResult> GetDayMarkers(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(new GetCycleDayMarkersQuery(from, to), ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("insight")]
    [Authorize(Policy = SubscriptionPolicies.RequirePro)]
    [EnableRateLimiting(RateLimitPolicyNames.Ai)]
    public async Task<IActionResult> GetInsight([FromQuery] string? lang, CancellationToken ct)
    {
        try
        {
            var language = string.Equals(lang, "vi", StringComparison.OrdinalIgnoreCase) ? "vi" : "en";
            return Ok(await mediator.Send(new GetCycleInsightQuery(language), ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("clients/{clientId:guid}/overview")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
    public async Task<IActionResult> GetClientOverview(Guid clientId, CancellationToken ct)
    {
        try
        {
            return Ok(await mediator.Send(new GetClientCycleOverviewQuery(clientId), ct));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            // Not shared / not female / no data → 404 so the coach UI hides the tile.
            return NotFound(new { message = ex.Message });
        }
    }
}
