using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.ExerciseTemplates.Commands.CreateCustomExerciseTemplate;
using Xenoh.Application.Features.ExerciseTemplates.Commands.CreateCustomExerciseTemplateForClient;
using Xenoh.Application.Features.ExerciseTemplates.Commands.DeleteCustomExerciseTemplate;
using Xenoh.Application.Features.ExerciseTemplates.Commands.UpdateCustomExerciseTemplate;
using Xenoh.Application.Features.ExerciseTemplates.Queries.GetClientExerciseTemplates;
using Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;
using Xenoh.Application.Features.ExerciseTemplates.Queries.GetLastExercisePerformance;
using Xenoh.Domain.Enums;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/exercise-templates")]
[Authorize]
public sealed class ExerciseTemplatesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] MuscleGroup? muscleGroup, CancellationToken ct)
    {
        var result = await mediator.Send(new GetExerciseTemplatesQuery(muscleGroup), ct);
        return Ok(result);
    }

    [HttpGet("{exerciseTemplateId:guid}/last-performance")]
    public async Task<IActionResult> GetLastPerformance(
        [FromRoute] Guid exerciseTemplateId,
        [FromQuery] Guid dailyWorkoutId,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(
                new GetLastExercisePerformanceQuery(exerciseTemplateId, dailyWorkoutId),
                ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("for-client/{clientId:guid}")]
    [Authorize(Roles = UserRole.Coach)]
    public async Task<IActionResult> GetForClient(
        [FromRoute] Guid clientId,
        [FromQuery] MuscleGroup? muscleGroup,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetClientExerciseTemplatesQuery(clientId, muscleGroup), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("custom")]
    public async Task<IActionResult> CreateCustom(
        [FromBody] CreateCustomExerciseTemplateCommand command,
        CancellationToken ct)
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

    [HttpPut("custom/{id:guid}")]
    public async Task<IActionResult> UpdateCustom(
        [FromRoute] Guid id,
        [FromBody] UpdateCustomExerciseTemplateCommand command,
        CancellationToken ct)
    {
        if (id != command.Id)
            return BadRequest(new { message = "Route id does not match request id." });

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

    [HttpDelete("custom/{id:guid}")]
    public async Task<IActionResult> DeleteCustom([FromRoute] Guid id, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new DeleteCustomExerciseTemplateCommand(id), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("custom/for-client/{clientId:guid}")]
    [Authorize(Roles = UserRole.Coach)]
    public async Task<IActionResult> CreateCustomForClient(
        [FromRoute] Guid clientId,
        [FromBody] CreateCustomExerciseTemplateForClientCommand command,
        CancellationToken ct)
    {
        if (clientId != command.ClientId)
            return BadRequest(new { message = "Route clientId does not match request clientId." });

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
