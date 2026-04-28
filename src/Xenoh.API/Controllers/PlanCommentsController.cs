using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.PlanComments.Commands.AddPlanComment;
using Xenoh.Application.Features.PlanComments.Commands.DeletePlanComment;
using Xenoh.Application.Features.PlanComments.Queries.GetPlanComments;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/plans/{planId:guid}/comments")]
[Authorize]
public sealed class PlanCommentsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetComments(Guid planId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPlanCommentsQuery(planId), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> AddComment(Guid planId, [FromBody] AddPlanCommentCommand command, CancellationToken ct)
    {
        try
        {
            var cmd = command with { PlanId = planId };
            var result = await mediator.Send(cmd, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(Guid planId, Guid commentId, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new DeletePlanCommentCommand(planId, commentId), ct);
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
