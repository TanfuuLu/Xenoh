using Mediator;

namespace Xenoh.Application.Features.Users.Queries.GetExercisePrs;

public sealed record GetExercisePrsQuery : IRequest<List<ExercisePrResponse>>;

public sealed record ExercisePrResponse(
    Guid ExerciseTemplateId,
    string ExerciseName,
    decimal CurrentWeight,
    int Reps,
    DateTime AchievedAt
);
