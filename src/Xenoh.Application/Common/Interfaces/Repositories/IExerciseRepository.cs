using Xenoh.Application.Features.Exercises.Commands.CreateExercise;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface IExerciseRepository
{
    Task<List<ExerciseResponse>> GetByDayWithPrsAsync(Guid dailyWorkoutId, Guid userId, CancellationToken ct = default);

    /// <summary>Tracked: exercise with sets + plan for permission + mutation.</summary>
    Task<Exercise?> FindWithSetsAndPlanAsync(Guid exerciseId, CancellationToken ct = default);

    /// <summary>Tracked: exercise with plan only (for delete — no sets needed).</summary>
    Task<Exercise?> FindWithPlanAsync(Guid exerciseId, CancellationToken ct = default);

    Task<int> GetNextSortOrderAsync(Guid dailyWorkoutId, CancellationToken ct = default);
    Task<List<Exercise>> GetByIdsWithPlanAsync(IEnumerable<Guid> exerciseIds, CancellationToken ct = default);

    Task AddAsync(Exercise exercise, CancellationToken ct = default);
    void Remove(Exercise exercise);
    void RemoveRange(IEnumerable<Exercise> exercises);
    void AddRange(IEnumerable<Exercise> exercises);
    void RemoveSetRange(IEnumerable<ExerciseSet> sets);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
