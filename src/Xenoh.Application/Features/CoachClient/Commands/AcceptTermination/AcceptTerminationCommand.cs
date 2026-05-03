using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.CoachClient.Commands.AcceptTermination;

public sealed record AcceptTerminationCommand : IRequest
{
    [Required]
    public required Guid RelationshipId { get; init; }
}
