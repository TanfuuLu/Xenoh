using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.TrainingDayShares.Commands.DeleteTrainingDayShare;
using Xenoh.Application.Features.TrainingDayShares.Commands.LoveTrainingDayShare;
using Xenoh.Application.Features.TrainingDayShares.Commands.ShareTrainingDay;
using Xenoh.Application.Features.TrainingDayShares.Commands.UnloveTrainingDayShare;
using Xenoh.Application.Features.TrainingDayShares.Queries.GetFriendTrainingDayFeed;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/training-day-shares")]
[Authorize]
public sealed class TrainingDaySharesController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Share([FromBody] ShareTrainingDayCommand command, CancellationToken ct)
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

    [HttpGet("feed")]
    public async Task<IActionResult> Feed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetFriendTrainingDayFeedQuery(page, pageSize), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/love")]
    public async Task<IActionResult> Love(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new LoveTrainingDayShareCommand(id), ct);
            return Ok(result);
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

    [HttpDelete("{id:guid}/love")]
    public async Task<IActionResult> Unlove(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new UnloveTrainingDayShareCommand(id), ct);
            return Ok(result);
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

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new DeleteTrainingDayShareCommand(id), ct);
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
}
