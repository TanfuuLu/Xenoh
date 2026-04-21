using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;

public sealed class GetExerciseTemplatesHandler(IApplicationDbContext context)
    : IRequestHandler<GetExerciseTemplatesQuery, List<ExerciseTemplateResponse>>
{
    public async ValueTask<List<ExerciseTemplateResponse>> Handle(
        GetExerciseTemplatesQuery request, CancellationToken cancellationToken)
    {
        var query = context.ExerciseTemplates.AsNoTracking();

        if (request.MuscleGroup is not null)
            query = query.Where(t => t.PrimaryMuscleGroup == request.MuscleGroup
                                  || t.SecondaryMuscleGroups.Contains(request.MuscleGroup.Value));

        return await query
            .OrderBy(t => t.PrimaryMuscleGroup)
            .ThenBy(t => t.Name)
            .Select(t => new ExerciseTemplateResponse(
                t.Id,
                t.Name,
                t.Description,
                t.PrimaryMuscleGroup.ToString(),
                t.SecondaryMuscleGroups.Select(m => m.ToString()).ToList()
            ))
            .ToListAsync(cancellationToken);
    }
}
