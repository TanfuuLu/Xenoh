using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces.Repositories;

public interface IExerciseSetRepository
{
    /// <summary>
    /// Tracked: set with the exercise, exercise sets, template, day, week, and plan
    /// needed by MarkSetComplete. Day/week summaries are computed with aggregate queries.
    /// </summary>
    Task<ExerciseSet?> FindForCompleteAsync(Guid setId, CancellationToken ct = default);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
