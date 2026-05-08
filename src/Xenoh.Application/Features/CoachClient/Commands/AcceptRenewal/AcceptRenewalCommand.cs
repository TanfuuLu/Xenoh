using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.CoachClient.Commands.AcceptRenewal;

public sealed record AcceptRenewalCommand : IRequest
{
    [Required]
    public required Guid RelationshipId { get; init; }
}
