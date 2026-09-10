using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Common.XP;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Exercises.Commands.UncompleteSet;

public sealed class UncompleteSetHandler(
    IExerciseSetRepository exerciseSetRepo,
    IUserPrRepository userPrRepo,
    IBodyweightRepository bodyweightRepo,
    IApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser
) : IRequestHandler<UncompleteSetCommand, ExerciseResponse>
{
    public async ValueTask<ExerciseResponse> Handle(UncompleteSetCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var set = await exerciseSetRepo.FindForCompleteAsync(request.SetId, cancellationToken)
            ?? throw new InvalidOperationException("Set not found.");
        var exercise = set.Exercise;
        var plan = exercise.DailyWorkout.WeeklyWorkout.Plan;
        if (plan.OwnerId != userId)
            throw new InvalidOperationException("Only the athlete can uncomplete a set.");

        set.IsCompleted = false;
        set.CompletedAt = null;
        exercise.IsCompleted = false;
        exercise.XpAwarded = false;
        exercise.UpdatedAt = DateTime.UtcNow;
        exercise.DailyWorkout.IsCompleted = await db.Exercises
            .Where(e => e.DailyWorkoutId == exercise.DailyWorkoutId)
            .AllAsync(e => e.Id != exercise.Id ? e.IsCompleted || e.IsSkipped : false, cancellationToken);
        exercise.DailyWorkout.UpdatedAt = DateTime.UtcNow;

        var hasCompletedOnDay = await db.ExerciseSets.AnyAsync(
            s => s.IsCompleted && s.Exercise.DailyWorkoutId == exercise.DailyWorkoutId,
            cancellationToken);
        if (!hasCompletedOnDay)
        {
            var history = await db.WorkoutHistories
                .Where(h => h.UserId == userId && h.Date == exercise.DailyWorkout.Date)
                .ToListAsync(cancellationToken);
            db.WorkoutHistories.RemoveRange(history);
        }

        var week = exercise.DailyWorkout.WeeklyWorkout;
        week.IsCompleted = await db.DailyWorkouts
            .Where(d => d.WeeklyWorkoutId == week.Id)
            .AllAsync(d => d.Id != exercise.DailyWorkoutId ? d.IsCompleted || d.Status != Domain.Enums.DayStatus.Normal : false, cancellationToken);
        week.UpdatedAt = DateTime.UtcNow;

        await exerciseSetRepo.SaveChangesAsync(cancellationToken);
        await RebuildResultsAsync(userId, exercise.ExerciseTemplateId, cancellationToken);
        await exerciseSetRepo.SaveChangesAsync(cancellationToken);

        var pr = (await userPrRepo.GetByTemplateIdsAsync(userId, [exercise.ExerciseTemplateId], cancellationToken))
            .GetValueOrDefault(exercise.ExerciseTemplateId);
        var bodyweight = await bodyweightRepo.GetLatestWeightOnOrBeforeAsync(plan.OwnerId, exercise.DailyWorkout.Date, cancellationToken);
        return CreateExerciseHandler.ToResponse(exercise, pr, bodyweight);
    }

    private async Task RebuildResultsAsync(Guid userId, Guid templateId, CancellationToken ct)
    {
        var completed = db.ExerciseSets
            .Where(s => s.IsCompleted && s.Exercise.ExerciseTemplateId == templateId && s.Exercise.DailyWorkout.WeeklyWorkout.Plan.OwnerId == userId);
        var best = await completed
            .Where(s => (s.ActualWeight ?? s.PlannedWeight) > 0)
            .OrderByDescending(s => s.ActualWeight ?? s.PlannedWeight)
            .ThenByDescending(s => s.ActualReps ?? s.PlannedReps)
            .Select(s => new { Weight = s.ActualWeight ?? s.PlannedWeight, Reps = s.ActualReps ?? s.PlannedReps, s.CompletedAt })
            .FirstOrDefaultAsync(ct);
        var pr = await db.UserExercisePRs.FirstOrDefaultAsync(p => p.UserId == userId && p.ExerciseTemplateId == templateId, ct);
        var oldHistory = await db.UserExercisePRHistories
            .Where(p => p.UserId == userId && p.ExerciseTemplateId == templateId)
            .ToListAsync(ct);
        db.UserExercisePRHistories.RemoveRange(oldHistory);
        if (best is null)
        {
            if (pr is not null) db.UserExercisePRs.Remove(pr);
        }
        else if (pr is null)
        {
            db.UserExercisePRs.Add(new UserExercisePR { UserId = userId, ExerciseTemplateId = templateId, Weight = best.Weight!.Value, Reps = best.Reps, AchievedAt = best.CompletedAt ?? DateTime.UtcNow });
        }
        else
        {
            pr.Weight = best.Weight!.Value;
            pr.Reps = best.Reps;
            pr.AchievedAt = best.CompletedAt ?? DateTime.UtcNow;
        }
        if (best is not null)
        {
            db.UserExercisePRHistories.Add(new UserExercisePRHistory
            {
                UserId = userId,
                ExerciseTemplateId = templateId,
                Weight = best.Weight!.Value,
                Reps = best.Reps,
                AchievedAt = best.CompletedAt ?? DateTime.UtcNow,
            });
        }

        var xpRows = await db.Exercises
            .Where(e => e.DailyWorkout.WeeklyWorkout.Plan.OwnerId == userId && e.IsCompleted && e.DurationSeconds > ExerciseXpAwarder.ValidExerciseDurationSeconds)
            .SelectMany(e => e.Sets.Where(s => s.IsCompleted).Select(s => new { s.ActualWeight, s.PlannedWeight, s.ActualReps, s.PlannedReps, IsCompetitionLift = e.ExerciseTemplate.IsCompetitionLift }))
            .ToListAsync(ct);
        var xp = xpRows.Sum(x => (long)XpCalculator.ComputeSetXp(x.ActualWeight, x.PlannedWeight, x.ActualReps, x.PlannedReps, x.IsCompetitionLift));
        var user = await userManager.FindByIdAsync(userId.ToString()) ?? throw new InvalidOperationException("User not found.");
        user.TotalXp = xp;
        user.Level = XpCalculator.ComputeLevel(xp);
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded) throw new InvalidOperationException("Failed to rebuild user XP.");
    }
}
