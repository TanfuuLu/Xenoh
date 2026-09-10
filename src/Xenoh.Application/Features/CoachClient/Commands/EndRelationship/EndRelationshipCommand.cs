using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.CoachClient.Commands.EndRelationship;

public sealed record EndRelationshipCommand : IRequest
{
    public bool Immediate { get; init; }
    public Guid? ExpectedRevision { get; init; }
    public DateTime? RequestedAtUtc { get; init; }
    public DateTime? EffectiveAtUtc { get; init; }
    [Required]
    public required Guid RelationshipId { get; init; }
}
