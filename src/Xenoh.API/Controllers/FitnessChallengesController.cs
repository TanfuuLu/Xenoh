using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.FitnessChallenges;

namespace Xenoh.API.Controllers;

[ApiController, Authorize, Route("api/community/challenges")]
public sealed class FitnessChallengesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? status, CancellationToken ct) => Ok(await mediator.Send(new GetFitnessChallengesQuery(status), ct));

    [HttpGet("invitees")]
    public async Task<IActionResult> Invitees(CancellationToken ct) => Ok(await mediator.Send(new GetChallengeInviteesQuery(), ct));

    [HttpGet("discover")]
    public async Task<IActionResult> Discover(CancellationToken ct) => Ok(await mediator.Send(new GetDiscoverableFitnessChallengesQuery(), ct));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => await Execute(() => mediator.Send(new GetFitnessChallengeQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FitnessChallengeInput input, CancellationToken ct) =>
        await Execute(() => mediator.Send(new CreateFitnessChallengeCommand(input), ct));

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] FitnessChallengeInput input, CancellationToken ct) =>
        await Execute(() => mediator.Send(new UpdateFitnessChallengeCommand(id, input), ct));

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct) => await Execute(() => mediator.Send(new AcceptFitnessChallengeCommand(id), ct));

    [HttpPost("{id:guid}/decline")]
    public async Task<IActionResult> Decline(Guid id, CancellationToken ct) => await ExecuteNoContent(() => mediator.Send(new DeclineFitnessChallengeCommand(id), ct));

    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id, CancellationToken ct) => await ExecuteNoContent(() => mediator.Send(new LeaveFitnessChallengeCommand(id), ct));

    [HttpPost("{id:guid}/join")]
    public async Task<IActionResult> Join(Guid id, CancellationToken ct) => await Execute(() => mediator.Send(new JoinFitnessChallengeCommand(id), ct));

    [HttpPost("{id:guid}/invites")]
    public async Task<IActionResult> Invite(Guid id, [FromBody] InviteMembersBody body, CancellationToken ct) =>
        await Execute(() => mediator.Send(new InviteFitnessChallengeMembersCommand(id, body.UserIds), ct));

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId, CancellationToken ct) =>
        await ExecuteNoContent(() => mediator.Send(new RemoveFitnessChallengeMemberCommand(id, userId), ct));

    [HttpPost("{id:guid}/check-ins")]
    public async Task<IActionResult> CheckIn(Guid id, [FromBody] ChallengeCheckInBody body, CancellationToken ct) =>
        await Execute(() => mediator.Send(new CheckInFitnessChallengeCommand(id, body.Note), ct));

    [HttpDelete("{id:guid}/check-ins/today")]
    public async Task<IActionResult> UndoCheckIn(Guid id, CancellationToken ct) =>
        await Execute(() => mediator.Send(new UndoFitnessChallengeCheckInCommand(id), ct));

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) => await ExecuteNoContent(() => mediator.Send(new CancelFitnessChallengeCommand(id), ct));

    public sealed record InviteMembersBody(IReadOnlyList<Guid> UserIds);
    public sealed record ChallengeCheckInBody(string? Note);

    private async Task<IActionResult> Execute<T>(Func<ValueTask<T>> action)
    {
        try { return Ok(await action()); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }

    private async Task<IActionResult> ExecuteNoContent(Func<ValueTask<Unit>> action)
    {
        try { await action(); return NoContent(); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
    }
}
