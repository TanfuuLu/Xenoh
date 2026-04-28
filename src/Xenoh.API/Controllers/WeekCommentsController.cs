using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.WeekComments.Commands.AddWeekComment;
using Xenoh.Application.Features.WeekComments.Commands.DeleteWeekComment;
using Xenoh.Application.Features.WeekComments.Queries.GetWeekComments;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/weeks/{weekId:guid}/comments")]
[Authorize]
public sealed class WeekCommentsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetComments(Guid weekId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetWeekCommentsQuery(weekId), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> AddComment(Guid weekId, [FromBody] AddWeekCommentCommand command, CancellationToken ct)
    {
        try
        {
            var cmd = command with { WeeklyWorkoutId = weekId };
            var result = await mediator.Send(cmd, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(Guid weekId, Guid commentId, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new DeleteWeekCommentCommand(weekId, commentId), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message.Contains("not found")
                ? NotFound(new { message = ex.Message })
                : BadRequest(new { message = ex.Message });
        }
    }
}
