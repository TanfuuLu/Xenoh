using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.DailyWorkouts.Queries.GetDaysByWeek;

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
}
