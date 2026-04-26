using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Users.Commands.DeleteBodyweightEntry;
using Xenoh.Application.Features.Users.Commands.LogBodyweight;
using Xenoh.Application.Features.Users.Commands.UpdateMyProfile;
using Xenoh.Application.Features.Users.Queries.GetBodyweightHistory;
using Xenoh.Application.Features.Users.Queries.GetMyProfile;
using Xenoh.Application.Features.Users.Queries.GetUserProfile;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController(IMediator mediator) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetMyProfileQuery(), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileCommand command, CancellationToken ct)
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

    [HttpPost("me/bodyweight")]
    public async Task<IActionResult> LogBodyweight([FromBody] LogBodyweightCommand command, CancellationToken ct)
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

    [HttpGet("me/bodyweight")]
    public async Task<IActionResult> GetBodyweightHistory(CancellationToken ct)
    {
        var result = await mediator.Send(new GetBodyweightHistoryQuery(), ct);
        return Ok(result);
    }

    [HttpDelete("me/bodyweight/{id:guid}")]
    public async Task<IActionResult> DeleteBodyweightEntry(Guid id, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new DeleteBodyweightEntryCommand(id), ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Coach views a client's full profile stats, or a client views their coach's profile.
    /// Requires an active coach–client relationship between caller and the target user.
    /// </summary>
    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetUserProfile(Guid userId, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetUserProfileQuery(userId), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
