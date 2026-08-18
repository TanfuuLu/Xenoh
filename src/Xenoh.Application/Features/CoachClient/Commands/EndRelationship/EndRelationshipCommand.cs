using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.CoachClient.Commands.EndRelationship;

public sealed record EndRelationshipCommand : IRequest
{
    [Required]
    public required Guid RelationshipId { get; init; }
}
