using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;

public sealed class GetExerciseTemplatesHandler(
    IExerciseTemplateRepository exerciseTemplateRepo,
    ICurrentUserService currentUser)
    : IRequestHandler<GetExerciseTemplatesQuery, IReadOnlyList<ExerciseTemplateResponse>>
{
    public ValueTask<IReadOnlyList<ExerciseTemplateResponse>> Handle(
        GetExerciseTemplatesQuery request, CancellationToken cancellationToken) =>
        new(exerciseTemplateRepo.GetAvailableForUserAsync(
            currentUser.UserId,
            request.MuscleGroup,
            cancellationToken));
}
