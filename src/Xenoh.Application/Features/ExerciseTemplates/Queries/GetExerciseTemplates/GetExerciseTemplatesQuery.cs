using Mediator;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;

public sealed record GetExerciseTemplatesQuery(MuscleGroup? MuscleGroup = null)
    : IRequest<List<ExerciseTemplateResponse>>;

public sealed record ExerciseTemplateResponse(
    Guid Id,
    string Name,
    string? Description,
    string PrimaryMuscleGroup,
    List<string> SecondaryMuscleGroups,
    string ExerciseKind,
    decimal EstimatedMet,
    bool IsCustom,
    Guid? OwnerId,
    string? ImageUrl
);
