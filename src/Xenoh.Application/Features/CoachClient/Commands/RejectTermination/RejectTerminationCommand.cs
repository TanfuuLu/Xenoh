using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.CoachClient.Commands.RejectTermination;

public sealed record RejectTerminationCommand : IRequest
{
    [Required]
    public required Guid RelationshipId { get; init; }
}
