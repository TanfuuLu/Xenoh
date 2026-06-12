using Mediator;
using Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.ExerciseTemplates.Queries.GetClientExerciseTemplates;

public sealed record GetClientExerciseTemplatesQuery(Guid ClientId, MuscleGroup? MuscleGroup = null)
    : IRequest<IReadOnlyList<ExerciseTemplateResponse>>;
