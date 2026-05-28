using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Common.Pagination;

namespace Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;

public sealed class GetExerciseTemplatesHandler(
    IExerciseTemplateRepository exerciseTemplateRepo,
    ICurrentUserService currentUser)
    : IRequestHandler<GetExerciseTemplatesQuery, PagedResponse<ExerciseTemplateResponse>>
{
    public async ValueTask<PagedResponse<ExerciseTemplateResponse>> Handle(
        GetExerciseTemplatesQuery request, CancellationToken cancellationToken) =>
        await exerciseTemplateRepo.GetAllAsync(
            currentUser.UserId,
            request.MuscleGroup,
            PaginationDefaults.NormalizePageNumber(request.PageNumber),
            PaginationDefaults.NormalizePageSize(request.PageSize),
            cancellationToken);
}
