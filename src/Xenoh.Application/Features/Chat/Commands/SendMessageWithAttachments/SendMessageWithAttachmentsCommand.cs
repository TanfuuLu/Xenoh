using Mediator;
using Xenoh.Application.Features.Chat.Dtos;

namespace Xenoh.Application.Features.Chat.Commands.SendMessageWithAttachments;

/// <summary>A single uploaded file, decoupled from ASP.NET's IFormFile.</summary>
public sealed record ChatAttachmentUpload(
    string FileName,
    string ContentType,
    long Length,
    Stream Content);

public sealed record SendMessageWithAttachmentsCommand : IRequest<MessageResponse>
{
    public Guid RelationshipId { get; init; }

    /// <summary>Optional text — may be empty when at least one file is attached.</summary>
    public string Content { get; init; } = string.Empty;

    public IReadOnlyList<ChatAttachmentUpload> Files { get; init; } = [];
}
