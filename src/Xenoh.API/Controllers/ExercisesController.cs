using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;
using Xenoh.Application.Features.Exercises.Commands.DeleteExercise;
using Xenoh.Application.Features.Exercises.Commands.FinishExerciseTimer;
using Xenoh.Application.Features.Exercises.Commands.SetExerciseTimerDuration;
using Xenoh.Application.Features.Exercises.Commands.MarkSetComplete;
using Xenoh.Application.Features.Exercises.Commands.ReorderExercises;
using Xenoh.Application.Features.Exercises.Commands.StartExerciseTimer;
using Xenoh.Application.Features.Exercises.Commands.UpdateExercise;
using Xenoh.Application.Features.Exercises.Queries.GetExercisesByDay;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/exercises")]
[Authorize]
public sealed class ExercisesController(IMediator mediator) : ControllerBase
{
    [HttpGet("by-day/{dailyWorkoutId:guid}")]
    public async Task<IActionResult> GetByDay(
        Guid dailyWorkoutId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        try
        {
            var result = await mediator.Send(new GetExercisesByDayQuery(dailyWorkoutId, pageNumber, pageSize), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPatch("by-day/{dailyWorkoutId:guid}/reorder")]
    public async Task<IActionResult> Reorder(
        Guid dailyWorkoutId,
        [FromBody] ReorderExercisesCommand command,
        CancellationToken ct)
    {
        if (dailyWorkoutId != command.DailyWorkoutId)
            return BadRequest(new { message = "DailyWorkoutId mismatch." });

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

    [HttpPatch("{exerciseId:guid}/timer/start")]
    public async Task<IActionResult> StartTimer(Guid exerciseId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new StartExerciseTimerCommand(exerciseId), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{exerciseId:guid}/timer/finish")]
    public async Task<IActionResult> FinishTimer(Guid exerciseId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new FinishExerciseTimerCommand(exerciseId), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{exerciseId:guid}/timer/set-duration")]
    public async Task<IActionResult> SetDuration(
        Guid exerciseId,
        [FromBody] SetExerciseTimerDurationCommand command,
        CancellationToken ct)
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
        try
        {
            var result = await mediator.Send(command with { SetId = setId }, ct);
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
