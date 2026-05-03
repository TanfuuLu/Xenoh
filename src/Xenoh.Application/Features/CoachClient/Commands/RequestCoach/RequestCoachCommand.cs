using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.CoachClient.Commands.RequestCoach;

public sealed record RequestCoachCommand : IRequest<CoachRelationshipResponse>
{
    [Required]
    public required Guid CoachId { get; init; }
}

public sealed record CoachRelationshipResponse(
    Guid Id,
    Guid ClientId,
    string ClientName,
    string? ClientAvatarUrl,
    Guid CoachId,
    string CoachName,
    string Status,
    DateTime CreatedAt,
    Guid? TerminationRequestedBy
);
