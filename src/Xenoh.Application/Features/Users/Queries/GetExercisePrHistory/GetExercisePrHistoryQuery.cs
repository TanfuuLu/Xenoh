using Mediator;

namespace Xenoh.Application.Features.Users.Queries.GetExercisePrHistory;

public sealed record GetExercisePrHistoryQuery(Guid ExerciseTemplateId) : IRequest<List<ExercisePrHistoryPointResponse>>;

public sealed record ExercisePrHistoryPointResponse(
    Guid ExerciseTemplateId,
    decimal Weight,
    int Reps,
    DateTime AchievedAt
);
