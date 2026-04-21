using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;
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
}
