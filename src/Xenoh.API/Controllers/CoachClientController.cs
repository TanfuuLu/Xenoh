using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.CoachClient.Commands.AcceptRenewal;
using Xenoh.Application.Features.CoachClient.Commands.AcceptRequest;
using Xenoh.Application.Features.CoachClient.Commands.AcceptTermination;
using Xenoh.Application.Features.CoachClient.Commands.RejectRenewal;
using Xenoh.Application.Features.CoachClient.Commands.RejectTermination;
using Xenoh.Application.Features.CoachClient.Commands.RequestCoach;
using Xenoh.Application.Features.CoachClient.Commands.RequestRenewal;
using Xenoh.Application.Features.CoachClient.Commands.RequestTermination;
using Xenoh.Application.Features.CoachClient.Commands.TerminateRelationship;
using Xenoh.Application.Features.CoachClient.Queries.GetClientPowerlifting;
using Xenoh.Application.Features.CoachClient.Queries.GetCoachDashboard;
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
    [Authorize(Roles = $"{UserRole.Individual},{UserRole.Coach}")]
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
    [Authorize(Roles = $"{UserRole.Individual},{UserRole.Coach}")]
    public async Task<IActionResult> GetMyCoach(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyCoachQuery(), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{relationshipId:guid}/request-termination")]
    public async Task<IActionResult> RequestTermination(Guid relationshipId, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new RequestTerminationCommand { RelationshipId = relationshipId }, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{relationshipId:guid}/accept-termination")]
    public async Task<IActionResult> AcceptTermination(Guid relationshipId, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new AcceptTerminationCommand { RelationshipId = relationshipId }, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{relationshipId:guid}/reject-termination")]
    public async Task<IActionResult> RejectTermination(Guid relationshipId, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new RejectTerminationCommand { RelationshipId = relationshipId }, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public sealed record RequestRenewalRequest(DateOnly ProposedEndDate);

    [HttpPost("{relationshipId:guid}/request-renewal")]
    public async Task<IActionResult> RequestRenewal(Guid relationshipId, [FromBody] RequestRenewalRequest body, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new RequestRenewalCommand
            {
                RelationshipId = relationshipId,
                ProposedEndDate = body.ProposedEndDate
            }, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{relationshipId:guid}/accept-renewal")]
    public async Task<IActionResult> AcceptRenewal(Guid relationshipId, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new AcceptRenewalCommand { RelationshipId = relationshipId }, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{relationshipId:guid}/reject-renewal")]
    public async Task<IActionResult> RejectRenewal(Guid relationshipId, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new RejectRenewalCommand { RelationshipId = relationshipId }, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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

    /// <summary>
    /// [Coach only] Dashboard: per-client stats — last workout, plan progress, Big 3 PRs, bodyweight.
    /// </summary>
    [HttpGet("dashboard")]
    [Authorize(Roles = UserRole.Coach)]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var result = await mediator.Send(new GetCoachDashboardQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// [Coach only] Powerlifting analytics for one of the coach's clients —
    /// per-lift e1RM series, PR timeline, current training max, DOTS-over-time.
    /// </summary>
    [HttpGet("clients/{clientId:guid}/powerlifting")]
    [Authorize(Roles = UserRole.Coach)]
    public async Task<IActionResult> GetClientPowerlifting(Guid clientId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetClientPowerliftingQuery(clientId), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
