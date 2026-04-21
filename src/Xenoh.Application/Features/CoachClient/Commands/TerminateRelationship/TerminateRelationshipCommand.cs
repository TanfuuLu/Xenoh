using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.CoachClient.Commands.TerminateRelationship;

public sealed record TerminateRelationshipCommand : IRequest
{
    [Required]
    public required Guid RelationshipId { get; init; }
}
