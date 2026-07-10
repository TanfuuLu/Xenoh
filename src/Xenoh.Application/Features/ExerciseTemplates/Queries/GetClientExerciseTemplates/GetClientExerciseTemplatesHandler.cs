using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;

namespace Xenoh.Application.Features.ExerciseTemplates.Queries.GetClientExerciseTemplates;

public sealed class GetClientExerciseTemplatesHandler(
    IExerciseTemplateRepository exerciseTemplateRepo,
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser,
    IApplicationCache? cache = null)
    : IRequestHandler<GetClientExerciseTemplatesQuery, IReadOnlyList<ExerciseTemplateResponse>>
{
    public async ValueTask<IReadOnlyList<ExerciseTemplateResponse>> Handle(
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

        if (cache is null)
            return await exerciseTemplateRepo.GetAvailableForUserAsync(request.ClientId, request.MuscleGroup, cancellationToken);

        return await cache.GetOrCreateAsync(
            CacheTags.Templates,
            $"user:{request.ClientId:N}:muscle:{request.MuscleGroup?.ToString() ?? "all"}",
            TimeSpan.FromMinutes(30),
            ct => exerciseTemplateRepo.GetAvailableForUserAsync(request.ClientId, request.MuscleGroup, ct),
            cancellationToken);
    }
}
