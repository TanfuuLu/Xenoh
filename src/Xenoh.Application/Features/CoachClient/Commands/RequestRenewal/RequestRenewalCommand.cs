using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.CoachClient.Commands.RequestRenewal;

public sealed record RequestRenewalCommand : IRequest
{
    [Required]
    public required Guid RelationshipId { get; init; }

    [Required]
    public required DateOnly ProposedEndDate { get; init; }
}
