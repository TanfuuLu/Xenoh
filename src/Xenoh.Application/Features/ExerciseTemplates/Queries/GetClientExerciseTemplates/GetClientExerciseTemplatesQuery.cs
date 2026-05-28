using Mediator;
using Xenoh.Application.Common.Pagination;
using Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.ExerciseTemplates.Queries.GetClientExerciseTemplates;

public sealed record GetClientExerciseTemplatesQuery(Guid ClientId, MuscleGroup? MuscleGroup = null, int PageNumber = 1, int PageSize = 20)
    : IRequest<PagedResponse<ExerciseTemplateResponse>>;
