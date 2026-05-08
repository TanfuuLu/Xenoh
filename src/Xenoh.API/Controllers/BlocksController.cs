using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Blocks.Commands.BlockUser;
using Xenoh.Application.Features.Blocks.Commands.UnblockUser;
using Xenoh.Application.Features.Blocks.Queries.GetMyBlocks;

namespace Xenoh.API.Controllers;

[ApiController]
[Authorize]
public sealed class BlocksController(IMediator mediator) : ControllerBase
{
    public sealed record BlockUserRequest(string? Reason);

    [HttpPost("api/users/{userId:guid}/block")]
    public async Task<IActionResult> Block(Guid userId, [FromBody] BlockUserRequest? body, CancellationToken ct)
    {
        try
        {
            await mediator.Send(new BlockUserCommand { TargetUserId = userId, Reason = body?.Reason }, ct);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("api/users/{userId:guid}/block")]
    public async Task<IActionResult> Unblock(Guid userId, CancellationToken ct)
    {
        await mediator.Send(new UnblockUserCommand { TargetUserId = userId }, ct);
        return NoContent();
    }

    [HttpGet("api/users/me/blocks")]
    public async Task<IActionResult> GetMyBlocks(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyBlocksQuery(), ct);
        return Ok(result);
    }
}
