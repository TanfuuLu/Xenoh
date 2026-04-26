using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Exercises.Commands.MarkSetComplete;

public sealed class MarkSetCompleteHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser
) : IRequestHandler<MarkSetCompleteCommand, ExerciseResponse>
{
    public async ValueTask<ExerciseResponse> Handle(MarkSetCompleteCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var set = await context.ExerciseSets
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
            .FirstOrDefaultAsync(s => s.Id == request.SetId, cancellationToken)
            ?? throw new InvalidOperationException("Set not found.");

        var exercise = set.Exercise;

        if (exercise.DailyWorkout.WeeklyWorkout.Plan.OwnerId != userId)
            throw new InvalidOperationException("Access denied.");

        if (set.IsCompleted)
            throw new InvalidOperationException("Set is already completed.");

        // Mark set done
        set.IsCompleted = true;
        set.CompletedAt = DateTime.UtcNow;
        set.UpdatedAt = DateTime.UtcNow;

        if (request.ActualReps is not null) set.ActualReps = request.ActualReps;
        if (request.ActualWeight is not null) set.ActualWeight = request.ActualWeight;
        if (request.Rpe is not null) set.Rpe = request.Rpe;

        // Auto-complete exercise when all sets are done
        bool allSetsDone = exercise.Sets.All(s => s.IsCompleted || s.Id == set.Id);
        exercise.IsCompleted = allSetsDone;
        exercise.UpdatedAt = DateTime.UtcNow;

        // Auto-complete daily workout when all exercises are done
        var dailyWorkout = exercise.DailyWorkout;
        bool allExercisesDone = dailyWorkout.Exercises.All(e =>
            e.Id == exercise.Id ? allSetsDone : e.IsCompleted);

        dailyWorkout.IsCompleted = allExercisesDone;
        dailyWorkout.UpdatedAt = DateTime.UtcNow;

        // Log workout history once per day (for streak tracking)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        bool alreadyLogged = await context.WorkoutHistories
            .AnyAsync(h => h.UserId == userId && h.Date == today, cancellationToken);
        if (!alreadyLogged)
        {
            context.WorkoutHistories.Add(new WorkoutHistory { UserId = userId, Date = today });
        }

        await context.SaveChangesAsync(cancellationToken);

        // PR upsert: use actual weight if provided, else fall back to planned weight
        decimal? prWeight = null;
        var effectiveWeight = set.ActualWeight ?? set.PlannedWeight;
        if (effectiveWeight is > 0)
        {
            var pr = await context.UserExercisePRs
                .FirstOrDefaultAsync(p => p.UserId == userId
                    && p.ExerciseTemplateId == exercise.ExerciseTemplateId, cancellationToken);

            if (pr is null)
            {
                pr = new UserExercisePR
                {
                    UserId = userId,
                    ExerciseTemplateId = exercise.ExerciseTemplateId,
                    Weight = effectiveWeight.Value,
                    Reps = set.ActualReps ?? set.PlannedReps,
                    AchievedAt = DateTime.UtcNow
                };
                context.UserExercisePRs.Add(pr);
                await context.SaveChangesAsync(cancellationToken);
            }
            else if (effectiveWeight.Value > pr.Weight)
            {
                pr.Weight = effectiveWeight.Value;
                pr.Reps = set.ActualReps ?? set.PlannedReps;
                pr.AchievedAt = DateTime.UtcNow;
                pr.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
            }

            prWeight = pr.Weight;
        }

        return CreateExerciseHandler.ToResponse(exercise, prWeight);
    }
}
