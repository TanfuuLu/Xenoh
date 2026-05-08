using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.CoachClient.Commands.RejectRenewal;

public sealed record RejectRenewalCommand : IRequest
{
    [Required]
    public required Guid RelationshipId { get; init; }
}
