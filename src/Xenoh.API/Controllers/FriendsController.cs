using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Friends.Commands.AcceptFriendRequest;
using Xenoh.Application.Features.Friends.Commands.RejectFriendRequest;
using Xenoh.Application.Features.Friends.Commands.RemoveFriend;
using Xenoh.Application.Features.Friends.Commands.SendFriendRequest;
using Xenoh.Application.Features.Friends.Queries.GetFriendRequests;
using Xenoh.Application.Features.Friends.Queries.GetFriends;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/friends")]
[Authorize]
public sealed class FriendsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetFriends(CancellationToken ct)
    {
        var result = await mediator.Send(new GetFriendsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("requests")]
    public async Task<IActionResult> GetRequests([FromQuery] string direction = "incoming", CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetFriendRequestsQuery(direction), ct);
        return Ok(result);
    }

    [HttpPost("requests")]
    public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestCommand command, CancellationToken ct)
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

    [HttpPost("requests/{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new AcceptFriendRequestCommand(id), ct);
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

    [HttpPost("requests/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new RejectFriendRequestCommand(id), ct);
            return NoContent();
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

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Remove(Guid userId, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new RemoveFriendCommand(userId), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
