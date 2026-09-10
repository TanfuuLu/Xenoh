using Mediator;

namespace Xenoh.Application.Features.Insights.Queries.GetTrainingComparison;

public sealed record GetTrainingComparisonQuery(
    int Days,
    Guid? ExerciseTemplateId) : IRequest<TrainingComparisonResponse>;
