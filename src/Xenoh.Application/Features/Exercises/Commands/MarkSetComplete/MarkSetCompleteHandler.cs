using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

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

        await context.SaveChangesAsync(cancellationToken);

        return CreateExerciseHandler.ToResponse(exercise);
    }
}
