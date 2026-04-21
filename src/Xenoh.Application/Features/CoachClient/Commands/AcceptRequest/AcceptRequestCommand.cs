using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Application.Features.CoachClient.Commands.RequestCoach;

namespace Xenoh.Application.Features.CoachClient.Commands.AcceptRequest;

public sealed record AcceptRequestCommand : IRequest<CoachRelationshipResponse>
{
    [Required]
    public required Guid RelationshipId { get; init; }
}
