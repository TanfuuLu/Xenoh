using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.CoachClient.Commands.AcceptRequest;

public sealed record AcceptRequestCommand : IRequest<CoachRelationshipResponse>
{
    [Required]
    public required Guid RelationshipId { get; init; }
}
