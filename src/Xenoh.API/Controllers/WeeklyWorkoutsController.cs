using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.WeeklyWorkouts.Commands.UpdateWeeklyWorkout;
using Xenoh.Application.Features.WeeklyWorkouts.Queries.GetWeeksByPlan;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/plans/{planId:guid}/weeks")]
[Authorize]
public sealed class WeeklyWorkoutsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetWeeks(Guid planId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetWeeksByPlanQuery(planId), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("{weeklyWorkoutId:guid}")]
    public async Task<IActionResult> UpdateName(
        Guid planId,
        Guid weeklyWorkoutId,
        [FromBody] UpdateWeeklyWorkoutCommand command,
        CancellationToken ct)
    {
        if (weeklyWorkoutId != command.WeeklyWorkoutId)
            return BadRequest(new { message = "WeeklyWorkoutId mismatch." });

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
