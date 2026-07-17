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

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => await Execute(() => mediator.Send(new GetFitnessChallengeQuery(id), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFitnessChallengeCommand command, CancellationToken ct) => await Execute(() => mediator.Send(command, ct));

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct) => await Execute(() => mediator.Send(new AcceptFitnessChallengeCommand(id), ct));

    [HttpPost("{id:guid}/decline")]
    public async Task<IActionResult> Decline(Guid id, CancellationToken ct) => await ExecuteNoContent(() => mediator.Send(new DeclineFitnessChallengeCommand(id), ct));

    [HttpPost("{id:guid}/leave")]
    public async Task<IActionResult> Leave(Guid id, CancellationToken ct) => await ExecuteNoContent(() => mediator.Send(new LeaveFitnessChallengeCommand(id), ct));

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) => await ExecuteNoContent(() => mediator.Send(new CancelFitnessChallengeCommand(id), ct));

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
