using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Common.XP;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Exercises.Commands.MarkSetComplete;

public sealed class MarkSetCompleteHandler(
    IExerciseSetRepository exerciseSetRepo,
    IWorkoutHistoryRepository workoutHistoryRepo,
    IUserPrRepository userPrRepo,
    ICurrentUserService currentUser,
    INotificationService notificationService,
    UserManager<ApplicationUser> userManager
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
            return CreateExerciseHandler.ToResponse(exercise, await GetPersonalRecordWeight(userId, exercise.ExerciseTemplateId, cancellationToken));

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
        bool allExercisesDone = dailyWorkout.Exercises.Any() && dailyWorkout.Exercises.All(e =>
            e.Id == exercise.Id ? allSetsDone : e.IsCompleted);

        dailyWorkout.IsCompleted = allExercisesDone;
        dailyWorkout.UpdatedAt = DateTime.UtcNow;

        // Auto-complete the week when all effective days are done / rest / missed
        var week = dailyWorkout.WeeklyWorkout;
        week.IsCompleted = IsWeekComplete(week);
        week.UpdatedAt = DateTime.UtcNow;

        // Log workout history once per day (for streak tracking)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        bool alreadyLogged = await workoutHistoryRepo.ExistsForDateAsync(userId, today, cancellationToken);
        if (!alreadyLogged)
            await workoutHistoryRepo.AddAsync(new WorkoutHistory { UserId = userId, Date = today }, cancellationToken);

        await ExerciseXpAwarder.AwardIfEligibleAsync(userManager, userId, exercise);
        await exerciseSetRepo.SaveChangesAsync(cancellationToken);

        // PR upsert: use actual weight if provided, else fall back to planned weight
        decimal? prWeight = null;
        var effectiveWeight = set.ActualWeight ?? set.PlannedWeight;
        if (effectiveWeight is > 0)
        {
            var pr = await userPrRepo.FindAsync(userId, exercise.ExerciseTemplateId, cancellationToken);
            var achievedAt = DateTime.UtcNow;
            var reps = set.ActualReps ?? set.PlannedReps;

            if (pr is null)
            {
                pr = new UserExercisePR
                {
                    UserId = userId,
                    ExerciseTemplateId = exercise.ExerciseTemplateId,
                    Weight = effectiveWeight.Value,
                    Reps = reps,
                    AchievedAt = achievedAt
                };
                await userPrRepo.AddAsync(pr, cancellationToken);
                await userPrRepo.AddHistoryAsync(CreateHistory(pr), cancellationToken);
                await userPrRepo.SaveChangesAsync(cancellationToken);
            }
            else if (effectiveWeight.Value > pr.Weight)
            {
                if (!await userPrRepo.HasHistoryAsync(userId, exercise.ExerciseTemplateId, cancellationToken))
                    await userPrRepo.AddHistoryAsync(CreateHistory(pr), cancellationToken);

                pr.Weight = effectiveWeight.Value;
                pr.Reps = reps;
                pr.AchievedAt = achievedAt;
                pr.UpdatedAt = achievedAt;
                await userPrRepo.AddHistoryAsync(CreateHistory(pr), cancellationToken);
                await userPrRepo.SaveChangesAsync(cancellationToken);
            }

            prWeight = pr.Weight;
        }

        // Notify coach of exercise warning (RPE >= 9 or < 70% of planned reps)
        var plan = exercise.DailyWorkout.WeeklyWorkout.Plan;
        if (plan.CreatedByCoachId.HasValue)
        {
            bool hasWarning =
                (set.Rpe.HasValue && set.Rpe.Value >= 9) ||
                (set.ActualReps.HasValue && set.ActualReps.Value < set.PlannedReps * 0.7m);

            if (hasWarning)
            {
                await notificationService.NotifyAsync(
                    plan.CreatedByCoachId.Value,
                    "ExerciseWarning",
                    $"Client có cảnh báo khi tập '{exercise.Name}' (ngày {exercise.DailyWorkout.Date:dd/MM/yyyy}).",
                    exercise.DailyWorkout.Id,
                    "Day",
                    cancellationToken);
            }
        }

        return CreateExerciseHandler.ToResponse(exercise, prWeight);
    }

    private static bool IsWeekComplete(WeeklyWorkout week)
    {
        var plan = week.Plan;
        var effective = week.DailyWorkouts
            .Where(d => d.Date >= plan.StartDate && d.Date <= plan.EndDate)
            .ToList();
        return effective.Count > 0 && effective.All(d =>
            d.IsCompleted || d.Status == DayStatus.Rest || d.Status == DayStatus.Missed);
    }

    private async Task<decimal?> GetPersonalRecordWeight(Guid userId, Guid exerciseTemplateId, CancellationToken cancellationToken)
    {
        var pr = await userPrRepo.FindAsync(userId, exerciseTemplateId, cancellationToken);
        return pr?.Weight;
    }

    private static UserExercisePRHistory CreateHistory(UserExercisePR pr) =>
        new()
        {
            UserId = pr.UserId,
            ExerciseTemplateId = pr.ExerciseTemplateId,
            Weight = pr.Weight,
            Reps = pr.Reps,
            AchievedAt = pr.AchievedAt
        };
}
