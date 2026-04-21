using Mediator;

namespace Xenoh.Application.Features.Coaches.Queries.GetCoaches;

public sealed record GetCoachesQuery(string? Name = null) : IRequest<List<CoachResponse>>;

public sealed record CoachResponse(
    Guid Id,
    string FullName,
    string Email
);
