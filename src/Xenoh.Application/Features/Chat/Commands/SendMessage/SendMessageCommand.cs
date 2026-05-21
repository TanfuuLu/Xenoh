using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Application.Features.Chat.Dtos;

namespace Xenoh.Application.Features.Chat.Commands.SendMessage;

public sealed record SendMessageCommand : IRequest<MessageResponse>
{
    public Guid RelationshipId { get; init; }

    [Required]
    [StringLength(2000, MinimumLength = 1)]
    public required string Content { get; init; }
}
