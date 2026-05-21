using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Chat.Commands.MarkMessagesRead;
using Xenoh.Application.Features.Chat.Commands.SendMessage;
using Xenoh.Application.Features.Chat.Queries.GetMessages;
using Xenoh.Application.Features.Chat.Queries.GetUnreadCounts;

namespace Xenoh.API.Controllers;

[ApiController]
[Route("api/messages")]
[Authorize]
public sealed class MessagesController(IMediator mediator) : ControllerBase
{
    [HttpGet("relationships/{relationshipId:guid}")]
    public async Task<IActionResult> GetMessages(
        Guid relationshipId,
        [FromQuery] int pageSize = 30,
        [FromQuery] DateTime? before = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await mediator.Send(new GetMessagesQuery(relationshipId, pageSize, before), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message.Contains("not found")
                ? NotFound(new { message = ex.Message })
                : BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("relationships/{relationshipId:guid}")]
    public async Task<IActionResult> SendMessage(
        Guid relationshipId,
        [FromBody] SendMessageCommand command,
        CancellationToken ct = default)
    {
        try
        {
            var cmd = command with { RelationshipId = relationshipId };
            var result = await mediator.Send(cmd, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("relationships/{relationshipId:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid relationshipId, CancellationToken ct)
    {
        await mediator.Send(new MarkMessagesReadCommand(relationshipId), ct);
        return NoContent();
    }

    [HttpGet("unread-counts")]
    public async Task<IActionResult> GetUnreadCounts(CancellationToken ct)
    {
        var result = await mediator.Send(new GetUnreadCountsQuery(), ct);
        return Ok(result);
    }
}
