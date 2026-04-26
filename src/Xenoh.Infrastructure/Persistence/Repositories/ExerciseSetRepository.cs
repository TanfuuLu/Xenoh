using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;

namespace Xenoh.Infrastructure.Persistence.Repositories;

public sealed class ExerciseSetRepository(ApplicationDbContext db) : IExerciseSetRepository
{
    public Task<ExerciseSet?> FindForCompleteAsync(Guid setId, CancellationToken ct) =>
        db.ExerciseSets
          .Include(s => s.Exercise)
              .ThenInclude(e => e.Sets)
          .Include(s => s.Exercise)
              .ThenInclude(e => e.DailyWorkout)
                  .ThenInclude(d => d.Exercises)
                      .ThenInclude(e => e.Sets)
          .Include(s => s.Exercise)
              .ThenInclude(e => e.DailyWorkout)
                  .ThenInclude(d => d.WeeklyWorkout)
                      .ThenInclude(w => w.Plan)
          .FirstOrDefaultAsync(s => s.Id == setId, ct);

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
