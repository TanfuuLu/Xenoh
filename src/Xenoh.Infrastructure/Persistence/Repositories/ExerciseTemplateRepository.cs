using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class ExerciseTemplateRepository(ApplicationDbContext db) : IExerciseTemplateRepository
{
    public Task<List<ExerciseTemplateResponse>> GetAllAsync(MuscleGroup? muscleGroup, CancellationToken ct)
    {
        var query = db.ExerciseTemplates.AsNoTracking();

        if (muscleGroup is not null)
            query = query.Where(t => t.PrimaryMuscleGroup == muscleGroup);

        return query
            .OrderBy(t => t.PrimaryMuscleGroup)
            .ThenBy(t => t.Name)
            .Select(t => new ExerciseTemplateResponse(
                t.Id,
                t.Name,
                t.Description,
                t.PrimaryMuscleGroup.ToString(),
                t.SecondaryMuscleGroups.Select(m => m.ToString()).ToList(),
                t.ExerciseKind.ToString(),
                t.EstimatedMet))
            .ToListAsync(ct);
    }

    public Task<ExerciseTemplate?> FindByIdAsync(Guid id, CancellationToken ct) =>
        db.ExerciseTemplates.FirstOrDefaultAsync(t => t.Id == id, ct);
}
