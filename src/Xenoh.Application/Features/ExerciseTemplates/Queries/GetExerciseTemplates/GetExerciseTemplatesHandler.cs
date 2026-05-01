using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;

public sealed class GetExerciseTemplatesHandler(
    IExerciseTemplateRepository exerciseTemplateRepo,
    ICurrentUserService currentUser)
    : IRequestHandler<GetExerciseTemplatesQuery, List<ExerciseTemplateResponse>>
{
    public async ValueTask<List<ExerciseTemplateResponse>> Handle(
        GetExerciseTemplatesQuery request, CancellationToken cancellationToken) =>
        await exerciseTemplateRepo.GetAllAsync(currentUser.UserId, request.MuscleGroup, cancellationToken);
}
