using Mediator;

namespace Xenoh.Application.Features.DailyWorkouts.Queries.GetDailyWorkoutGuidance;

public sealed record GetDailyWorkoutGuidanceQuery(Guid DailyWorkoutId, string? Language)
    : IRequest<WorkoutGuidanceResponse>;

public sealed record WorkoutGuidanceResponse(
    string Language,
    DateTime GeneratedAt,
    bool Cached,
    string Headline,
    string Readiness,
    IReadOnlyList<string> RecommendedAdjustments,
    IReadOnlyList<string> CautionFlags,
    IReadOnlyList<string> NextBestActions
);
