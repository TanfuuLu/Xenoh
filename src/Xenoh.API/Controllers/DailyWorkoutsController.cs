using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.DailyWorkouts.Commands.CopyDailyWorkout;
using Xenoh.Application.Features.DailyWorkouts.Commands.MarkDayStatus;
using Xenoh.Application.Features.DailyWorkouts.Queries.GetDailyWorkoutGuidance;
using Xenoh.Application.Features.DailyWorkouts.Queries.GetDaysByWeek;
using Xenoh.Domain.Enums;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/weeks/{weeklyWorkoutId:guid}/days")]
[Authorize]
public sealed class DailyWorkoutsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetDays(Guid weeklyWorkoutId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetDaysByWeekQuery(weeklyWorkoutId), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("/api/days/{dailyWorkoutId:guid}/status")]
    public async Task<IActionResult> MarkStatus(Guid dailyWorkoutId, [FromBody] MarkDayStatusRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<DayStatus>(request.Status, ignoreCase: true, out var status))
            return BadRequest(new { message = $"Invalid status '{request.Status}'. Allowed: Normal, Rest, Missed." });

        try
        {
            await mediator.Send(new MarkDayStatusCommand(dailyWorkoutId, status), ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("/api/days/{dailyWorkoutId:guid}/ai-guidance")]
    public async Task<IActionResult> GetAiGuidance(
        Guid dailyWorkoutId,
        [FromQuery] string? lang,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetDailyWorkoutGuidanceQuery(dailyWorkoutId, lang), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Copy all exercises from a source day to a target day.
    /// Existing exercises in the target day are replaced.
    /// </summary>
    [HttpPost("/api/days/{sourceDailyWorkoutId:guid}/copy")]
    public async Task<IActionResult> Copy(Guid sourceDailyWorkoutId, [FromBody] CopyDailyWorkoutRequest request, CancellationToken ct)
    {
        try
        {
            var command = new CopyDailyWorkoutCommand(sourceDailyWorkoutId, request.TargetDailyWorkoutId);
            var result = await mediator.Send(command, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
