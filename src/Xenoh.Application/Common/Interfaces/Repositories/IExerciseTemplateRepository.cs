using Xenoh.Application.Features.ExerciseTemplates.Queries.GetExerciseTemplates;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface IExerciseTemplateRepository
{
    // The exercise library is a bounded set, so it's loaded eagerly (no paging):
    // the full list of available templates is returned in one round-trip, custom
    // exercises first.
    Task<IReadOnlyList<ExerciseTemplateResponse>> GetAvailableForUserAsync(Guid userId, MuscleGroup? muscleGroup, CancellationToken ct = default);
    Task<ExerciseTemplate?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<ExerciseTemplate?> FindAvailableByIdAsync(Guid id, Guid userId, CancellationToken ct = default);
}
