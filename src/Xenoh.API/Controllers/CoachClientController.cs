using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.CoachClient.Commands.AcceptRequest;
using Xenoh.Application.Features.CoachClient.Commands.RequestCoach;
using Xenoh.Application.Features.CoachClient.Commands.TerminateRelationship;
using Xenoh.Application.Features.CoachClient.Queries.GetMyClients;
using Xenoh.Application.Features.CoachClient.Queries.GetMyCoach;
using Xenoh.Application.Features.CoachClient.Queries.GetPendingRequests;
using Xenoh.Domain.Enums;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/coach-client")]
[Authorize]
public sealed class CoachClientController(IMediator mediator) : ControllerBase
{
    [HttpPost("request")]
    [Authorize(Roles = UserRole.Individual)]
    public async Task<IActionResult> RequestCoach([FromBody] RequestCoachCommand command, CancellationToken ct)
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

    [HttpPut("accept/{relationshipId:guid}")]
    [Authorize(Roles = UserRole.Coach)]
    public async Task<IActionResult> AcceptRequest(Guid relationshipId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new AcceptRequestCommand { RelationshipId = relationshipId }, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{relationshipId:guid}")]
    public async Task<IActionResult> Terminate(Guid relationshipId, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new TerminateRelationshipCommand { RelationshipId = relationshipId }, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("pending-requests")]
    [Authorize(Roles = UserRole.Coach)]
    public async Task<IActionResult> GetPendingRequests(CancellationToken ct)
    {
        var result = await mediator.Send(new GetPendingRequestsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("my-coach")]
    [Authorize(Roles = UserRole.Individual)]
    public async Task<IActionResult> GetMyCoach(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyCoachQuery(), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// [Coach only] Trả về danh sách tất cả clients (Pending + Active).
    /// </summary>
    [HttpGet("my-clients")]
    [Authorize(Roles = UserRole.Coach)]
    public async Task<IActionResult> GetMyClients(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyClientsQuery(), ct);
        return Ok(result);
    }
}
