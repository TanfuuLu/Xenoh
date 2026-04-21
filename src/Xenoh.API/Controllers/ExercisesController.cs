using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;
using Xenoh.Application.Features.Exercises.Commands.DeleteExercise;
using Xenoh.Application.Features.Exercises.Commands.MarkSetComplete;
using Xenoh.Application.Features.Exercises.Commands.UpdateExercise;
using Xenoh.Application.Features.Exercises.Queries.GetExercisesByDay;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/exercises")]
[Authorize]
public sealed class ExercisesController(IMediator mediator) : ControllerBase
{
    [HttpGet("by-day/{dailyWorkoutId:guid}")]
    public async Task<IActionResult> GetByDay(Guid dailyWorkoutId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetExercisesByDayQuery(dailyWorkoutId), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExerciseCommand command, CancellationToken ct)
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

    [HttpPut("{exerciseId:guid}")]
    public async Task<IActionResult> Update(Guid exerciseId, [FromBody] UpdateExerciseCommand command, CancellationToken ct)
    {
        if (exerciseId != command.ExerciseId)
            return BadRequest(new { message = "ExerciseId mismatch." });
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

    /// <summary>
    /// Mark a single set as completed. Auto-completes Exercise and DailyWorkout when all sets/exercises are done.
    /// </summary>
    [HttpPatch("sets/{setId:guid}/complete")]
    public async Task<IActionResult> MarkSetComplete(Guid setId, [FromBody] MarkSetCompleteCommand command, CancellationToken ct)
    {
        if (setId != command.SetId)
            return BadRequest(new { message = "SetId mismatch." });
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

    [HttpDelete("{exerciseId:guid}")]
    public async Task<IActionResult> Delete(Guid exerciseId, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new DeleteExerciseCommand { ExerciseId = exerciseId }, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
