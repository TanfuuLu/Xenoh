using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.CoachClient.Commands.RequestTermination;

public sealed record RequestTerminationCommand : IRequest
{
    [Required]
    public required Guid RelationshipId { get; init; }
}
