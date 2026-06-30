using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xenoh.Application.Features.Chat.Commands.MarkMessagesRead;
using Xenoh.Application.Features.Chat.Commands.SendMessage;
using Xenoh.Application.Features.Chat.Commands.SendMessageWithAttachments;
using Xenoh.Application.Features.Chat.Queries.GetAttachmentUrl;
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

    private const long MaxUploadBytes = 25 * 1024 * 1024;

    /// <summary>Send a message with one or more file attachments (multipart).</summary>
    [HttpPost("relationships/{relationshipId:guid}/attachments")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> SendMessageWithAttachments(
        Guid relationshipId,
        [FromForm] string? content,
        [FromForm] IFormFileCollection? files,
        CancellationToken ct = default)
    {
        var streams = new List<Stream>();
        try
        {
            var uploads = new List<ChatAttachmentUpload>();
            foreach (var file in files ?? (IFormFileCollection)new FormFileCollection())
            {
                var stream = file.OpenReadStream();
                streams.Add(stream);
                uploads.Add(new ChatAttachmentUpload(file.FileName, file.ContentType, file.Length, stream));
            }

            var result = await mediator.Send(new SendMessageWithAttachmentsCommand
            {
                RelationshipId = relationshipId,
                Content = content ?? string.Empty,
                Files = uploads,
            }, ct);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        finally
        {
            foreach (var stream in streams)
                await stream.DisposeAsync();
        }
    }

    /// <summary>A short-lived presigned URL for a chat attachment (participants only).</summary>
    [HttpGet("attachments/{attachmentId:guid}/download-url")]
    public async Task<IActionResult> GetAttachmentUrl(
        Guid attachmentId, [FromQuery] bool inline = false, CancellationToken ct = default)
    {
        try
        {
            var result = await mediator.Send(new GetAttachmentUrlQuery(attachmentId, inline), ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message.Contains("not found")
                ? NotFound(new { message = ex.Message })
                : BadRequest(new { message = ex.Message });
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
