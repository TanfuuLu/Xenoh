using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Exercises.Commands.SkipExercise;

public sealed class SkipExerciseHandler(
    IExerciseRepository exerciseRepo,
    IBodyweightRepository bodyweightRepo,
    IUserPrRepository userPrRepo,
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<SkipExerciseCommand, ExerciseResponse>
{
    public async ValueTask<ExerciseResponse> Handle(SkipExerciseCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var exercise = await exerciseRepo.FindWithSetsAndPlanAsync(request.ExerciseId, cancellationToken)
            ?? throw new InvalidOperationException("Exercise not found.");

        var dailyWorkout = exercise.DailyWorkout;
        var week = dailyWorkout.WeeklyWorkout;
        var plan = week.Plan;

        if (plan.OwnerId != userId)
            throw new InvalidOperationException("Access denied.");

        if (request.IsSkipped)
        {
            if (exercise.IsCompleted)
                throw new InvalidOperationException("Cannot skip a completed exercise.");

            if (exercise.Sets.Any(s => s.IsCompleted))
                throw new InvalidOperationException("Cannot skip an exercise after sets have been completed.");
        }

        var now = DateTime.UtcNow;
        exercise.IsSkipped = request.IsSkipped;
        if (request.IsSkipped)
        {
            exercise.StartedAtUtc = null;
            exercise.EndedAtUtc = null;
            exercise.DurationSeconds = null;
        }
        exercise.UpdatedAt = now;

        var dayResolved = await IsDayResolvedAsync(
            dailyWorkout.Id,
            exercise.Id,
            exercise.IsCompleted,
            request.IsSkipped,
            cancellationToken);

        dailyWorkout.IsCompleted = dayResolved;
        dailyWorkout.UpdatedAt = now;

        week.IsCompleted = await IsWeekCompleteAsync(week, dailyWorkout.Id, dayResolved, cancellationToken);
        week.UpdatedAt = now;

        await exerciseRepo.SaveChangesAsync(cancellationToken);

        var prWeight = (await userPrRepo.GetByTemplateIdsAsync(
            userId, [exercise.ExerciseTemplateId], cancellationToken))
            .GetValueOrDefault(exercise.ExerciseTemplateId);

        var bodyweight = await bodyweightRepo.GetLatestWeightOnOrBeforeAsync(
            plan.OwnerId,
            dailyWorkout.Date,
            cancellationToken);

        return CreateExerciseHandler.ToResponse(exercise, prWeight, bodyweight);
    }

    private async Task<bool> IsDayResolvedAsync(
        Guid dailyWorkoutId,
        Guid updatedExerciseId,
        bool updatedExerciseIsCompleted,
        bool updatedExerciseIsSkipped,
        CancellationToken cancellationToken)
    {
        var exercises = await db.Exercises
            .AsNoTracking()
            .Where(e => e.DailyWorkoutId == dailyWorkoutId)
            .Select(e => new
            {
                e.Id,
                e.IsCompleted,
                e.IsSkipped
            })
            .ToListAsync(cancellationToken);

        return exercises.Count > 0 && exercises.All(e =>
        {
            var isCompleted = e.Id == updatedExerciseId ? updatedExerciseIsCompleted : e.IsCompleted;
            var isSkipped = e.Id == updatedExerciseId ? updatedExerciseIsSkipped : e.IsSkipped;

            return isCompleted || isSkipped;
        });
    }

    private async Task<bool> IsWeekCompleteAsync(
        Domain.Entities.WeeklyWorkout week,
        Guid updatedDailyWorkoutId,
        bool updatedDailyWorkoutIsCompleted,
        CancellationToken cancellationToken)
    {
        var plan = week.Plan;
        var effective = await db.DailyWorkouts
            .AsNoTracking()
            .Where(d =>
                d.WeeklyWorkoutId == week.Id &&
                d.Date >= plan.StartDate &&
                d.Date <= plan.EndDate)
            .Select(d => new
            {
                d.Id,
                d.IsCompleted,
                d.Status
            })
            .ToListAsync(cancellationToken);

        return effective.Count > 0 && effective.All(d =>
        {
            var isCompleted = d.Id == updatedDailyWorkoutId
                ? updatedDailyWorkoutIsCompleted
                : d.IsCompleted;

            return isCompleted || d.Status == DayStatus.Rest || d.Status == DayStatus.Missed;
        });
    }
}
