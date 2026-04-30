using Mediator;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Queries.GetCoachDashboard;

public sealed record GetCoachDashboardQuery : IRequest<List<CoachClientDashboardResponse>>;

public sealed record CoachClientDashboardResponse(
    Guid ClientId,
    string FullName,
    string Email,
    string? AvatarUrl,
    DateOnly? LastWorkoutDate,
    int? PlanProgressPercent,
    decimal? LatestBodyweightKg,
    BigThreePRs BigThreePRs
);

public sealed record BigThreePRs(
    decimal? Squat,
    decimal? Bench,
    decimal? Deadlift
);
