using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Common.Pagination;
using Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;

namespace Xenoh.Application.Features.ExerciseTemplates.Queries.GetClientExerciseTemplates;

public sealed class GetClientExerciseTemplatesHandler(
    IExerciseTemplateRepository exerciseTemplateRepo,
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser)
    : IRequestHandler<GetClientExerciseTemplatesQuery, PagedResponse<ExerciseTemplateResponse>>
{
    public async ValueTask<PagedResponse<ExerciseTemplateResponse>> Handle(
        GetClientExerciseTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;
        if (coachId == Guid.Empty)
            throw new InvalidOperationException("User is not authenticated.");

        var relationship = await coachClientRepo.FindActiveByCoachAndClientAsync(
            coachId,
            request.ClientId,
            cancellationToken);

        if (relationship is null)
            throw new InvalidOperationException("Client not found or no active coaching relationship.");

        return await exerciseTemplateRepo.GetAvailableForUserAsync(
            request.ClientId,
            request.MuscleGroup,
            PaginationDefaults.NormalizePageNumber(request.PageNumber),
            PaginationDefaults.NormalizePageSize(request.PageSize),
            cancellationToken);
    }
}
