using Mediator;

namespace Xenoh.Application.Features.Coaches.Queries.GetCoachProfile;

public sealed record GetCoachProfileQuery(Guid CoachId) : IRequest<CoachProfileResponse>;

public sealed record CoachProfileResponse(
    Guid Id,
    string FullName,
    string Email,
    string? AvatarUrl,
    int TotalClients
);
