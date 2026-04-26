using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Exercises.Commands.MarkSetComplete;

public sealed class MarkSetCompleteHandler(
    IExerciseSetRepository exerciseSetRepo,
    IWorkoutHistoryRepository workoutHistoryRepo,
    IUserPrRepository userPrRepo,
    ICurrentUserService currentUser
) : IRequestHandler<MarkSetCompleteCommand, ExerciseResponse>
{
    public async ValueTask<ExerciseResponse> Handle(MarkSetCompleteCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var set = await exerciseSetRepo.FindForCompleteAsync(request.SetId, cancellationToken)
            ?? throw new InvalidOperationException("Set not found.");

        var exercise = set.Exercise;

        if (exercise.DailyWorkout.WeeklyWorkout.Plan.OwnerId != userId)
            throw new InvalidOperationException("Access denied.");

        if (set.IsCompleted)
            throw new InvalidOperationException("Set is already completed.");

        set.IsCompleted = true;
        set.CompletedAt = DateTime.UtcNow;
        set.UpdatedAt = DateTime.UtcNow;

        if (request.ActualReps is not null) set.ActualReps = request.ActualReps;
        if (request.ActualWeight is not null) set.ActualWeight = request.ActualWeight;
        if (request.Rpe is not null) set.Rpe = request.Rpe;

        bool allSetsDone = exercise.Sets.All(s => s.IsCompleted || s.Id == set.Id);
        exercise.IsCompleted = allSetsDone;
        exercise.UpdatedAt = DateTime.UtcNow;

        var dailyWorkout = exercise.DailyWorkout;
        bool allExercisesDone = dailyWorkout.Exercises.All(e =>
            e.Id == exercise.Id ? allSetsDone : e.IsCompleted);

        dailyWorkout.IsCompleted = allExercisesDone;
        dailyWorkout.UpdatedAt = DateTime.UtcNow;

        // Log workout history once per day (for streak tracking)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        bool alreadyLogged = await workoutHistoryRepo.ExistsForDateAsync(userId, today, cancellationToken);
        if (!alreadyLogged)
            await workoutHistoryRepo.AddAsync(new WorkoutHistory { UserId = userId, Date = today }, cancellationToken);

        await exerciseSetRepo.SaveChangesAsync(cancellationToken);

        // PR upsert: use actual weight if provided, else fall back to planned weight
        decimal? prWeight = null;
        var effectiveWeight = set.ActualWeight ?? set.PlannedWeight;
        if (effectiveWeight is > 0)
        {
            var pr = await userPrRepo.FindAsync(userId, exercise.ExerciseTemplateId, cancellationToken);

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
                await userPrRepo.AddAsync(pr, cancellationToken);
                await userPrRepo.SaveChangesAsync(cancellationToken);
            }
            else if (effectiveWeight.Value > pr.Weight)
            {
                pr.Weight = effectiveWeight.Value;
                pr.Reps = set.ActualReps ?? set.PlannedReps;
                pr.AchievedAt = DateTime.UtcNow;
                pr.UpdatedAt = DateTime.UtcNow;
                await userPrRepo.SaveChangesAsync(cancellationToken);
            }

            prWeight = pr.Weight;
        }

        return CreateExerciseHandler.ToResponse(exercise, prWeight);
    }
}
