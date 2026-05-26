namespace Xenoh.Application.Features.CoachClient;

public sealed record CoachRelationshipResponse(
    Guid Id,
    Guid ClientId,
    string ClientName,
    string? ClientAvatarUrl,
    Guid CoachId,
    string CoachName,
    string Status,
    DateTime CreatedAt,
    Guid? TerminationRequestedBy,
    DateOnly StartDate,
    DateOnly? EndDate,
    Guid? RenewalRequestedBy,
    DateOnly? ProposedEndDate
);
