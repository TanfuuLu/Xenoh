using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Xenoh.API.Security;
using Xenoh.API.Auth;
using Xenoh.Application.Features.CoachClient.Commands.AcceptRenewal;
using Xenoh.Application.Features.CoachClient.Commands.AcceptRequest;
using Xenoh.Application.Features.CoachClient.Commands.ConnectByInviteCode;
using Xenoh.Application.Features.CoachClient.Commands.DeleteInviteCode;
using Xenoh.Application.Features.CoachClient.Commands.EndRelationship;
using Xenoh.Application.Features.CoachClient.Commands.GenerateInviteCode;
using Xenoh.Application.Features.CoachClient.Commands.RejectRenewal;
using Xenoh.Application.Features.CoachClient.Commands.RequestRenewal;
using Xenoh.Application.Features.CoachClient.Commands.TerminateRelationship;
using Xenoh.Application.Features.CoachClient.Queries.GetCoachClientAiBrief;
using Xenoh.Application.Features.CoachClient.Queries.GetClientPowerlifting;
using Xenoh.Application.Features.CoachClient.Queries.GetCoachDashboard;
using Xenoh.Application.Features.CoachClient.Queries.GetMyClients;
using Xenoh.Application.Features.CoachClient.Queries.GetMyCoach;
using Xenoh.Application.Features.CoachClient.Queries.GetMyInviteCodes;
using Xenoh.Application.Features.CoachClient.Queries.GetPendingRequests;
using Xenoh.Domain.Enums;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/coach-client")]
[Authorize]
public sealed class CoachClientController(IMediator mediator) : ControllerBase
{
    [HttpPut("accept/{relationshipId:guid}")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
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
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
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
        // Returns 200 with a null body when the client has no coach — the endpoint
        // is nullable, so "no coach" is a normal state, not a 404.
        return Ok(result);
    }

    /// <summary>
    /// Ends the relationship immediately. Either participant may call it; the other
    /// party is notified, not asked to approve.
    /// </summary>
    [HttpPost("{relationshipId:guid}/end")]
    public async Task<IActionResult> End(Guid relationshipId, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new EndRelationshipCommand { RelationshipId = relationshipId }, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Deprecated alias for <see cref="End"/>, kept for clients released before
    /// disconnecting became one-sided. Remove once web and mobile are both updated.</summary>
    [HttpPost("{relationshipId:guid}/request-termination")]
    [HttpPost("{relationshipId:guid}/accept-termination")]
    public Task<IActionResult> EndLegacy(Guid relationshipId, CancellationToken ct)
        => End(relationshipId, ct);

    /// <summary>Deprecated. There is no termination request to reject any more.</summary>
    [HttpPost("{relationshipId:guid}/reject-termination")]
    public IActionResult RejectTermination(Guid relationshipId)
        => StatusCode(StatusCodes.Status410Gone, new
        {
            message = "Disconnecting no longer needs approval, so there is nothing to reject."
        });

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
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
    public async Task<IActionResult> GetMyClients(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyClientsQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// [Coach only] Dashboard: per-client stats — last workout, plan progress, Big 3 PRs, bodyweight.
    /// </summary>
    [HttpGet("dashboard")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
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
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
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

    [HttpGet("clients/{clientId:guid}/ai-brief")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
    [EnableRateLimiting(RateLimitPolicyNames.Ai)]
    public async Task<IActionResult> GetClientAiBrief(
        Guid clientId,
        [FromQuery] string? lang,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetCoachClientAiBriefQuery(clientId, lang), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ─── Invite Codes ────────────────────────────────────────────────────────

    /// <summary>
    /// [Coach only] Generate a one-time invite code with a coaching period.
    /// </summary>
    [HttpPost("invite-codes")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
    public async Task<IActionResult> GenerateInviteCode(
        [FromBody] GenerateInviteCodeCommand command, CancellationToken ct)
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

    /// <summary>
    /// [Coach only] List all invite codes the coach has generated.
    /// </summary>
    [HttpGet("invite-codes")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
    public async Task<IActionResult> GetMyInviteCodes(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyInviteCodesQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// [Coach only] Delete an unused invite code.
    /// </summary>
    [HttpDelete("invite-codes/{id:guid}")]
    [Authorize(Policy = SubscriptionPolicies.RequireProCoach)]
    public async Task<IActionResult> DeleteInviteCode(Guid id, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new DeleteInviteCodeCommand { InviteCodeId = id }, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// [Any authenticated user] Connect with a coach using an invite code.
    /// Available to all plan tiers — no Pro subscription required.
    /// </summary>
    [HttpPost("connect-by-code")]
    public async Task<IActionResult> ConnectByCode(
        [FromBody] ConnectByInviteCodeCommand command, CancellationToken ct)
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
}
