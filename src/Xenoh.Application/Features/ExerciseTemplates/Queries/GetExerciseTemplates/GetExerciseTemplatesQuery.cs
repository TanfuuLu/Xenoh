using Mediator;
using Xenoh.Application.Common.Pagination;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;

public sealed record GetExerciseTemplatesQuery(MuscleGroup? MuscleGroup = null, int PageNumber = 1, int PageSize = 20)
    : IRequest<PagedResponse<ExerciseTemplateResponse>>;

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
