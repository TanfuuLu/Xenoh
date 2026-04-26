using Mediator;
using Xenoh.Application.Common.Interfaces.Repositories;

namespace Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;

public sealed class GetExerciseTemplatesHandler(IExerciseTemplateRepository exerciseTemplateRepo)
    : IRequestHandler<GetExerciseTemplatesQuery, List<ExerciseTemplateResponse>>
{
    public async ValueTask<List<ExerciseTemplateResponse>> Handle(
        GetExerciseTemplatesQuery request, CancellationToken cancellationToken) =>
        await exerciseTemplateRepo.GetAllAsync(request.MuscleGroup, cancellationToken);
}
