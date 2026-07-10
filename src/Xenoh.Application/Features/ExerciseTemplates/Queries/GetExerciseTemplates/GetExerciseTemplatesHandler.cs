using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;

public sealed class GetExerciseTemplatesHandler(
    IExerciseTemplateRepository exerciseTemplateRepo,
    ICurrentUserService currentUser,
    IApplicationCache? cache = null)
    : IRequestHandler<GetExerciseTemplatesQuery, IReadOnlyList<ExerciseTemplateResponse>>
{
    public ValueTask<IReadOnlyList<ExerciseTemplateResponse>> Handle(
        GetExerciseTemplatesQuery request, CancellationToken cancellationToken) =>
        new(cache is null
            ? exerciseTemplateRepo.GetAvailableForUserAsync(currentUser.UserId, request.MuscleGroup, cancellationToken)
            : cache.GetOrCreateAsync(
                CacheTags.Templates,
                $"user:{currentUser.UserId:N}:muscle:{request.MuscleGroup?.ToString() ?? "all"}",
                TimeSpan.FromMinutes(30),
                ct => exerciseTemplateRepo.GetAvailableForUserAsync(currentUser.UserId, request.MuscleGroup, ct),
                cancellationToken));
}
