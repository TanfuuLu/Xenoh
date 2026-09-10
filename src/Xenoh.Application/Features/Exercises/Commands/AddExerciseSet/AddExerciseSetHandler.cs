using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Exercises.Commands.AddExerciseSet;

public sealed class AddExerciseSetHandler(
    IExerciseRepository exerciseRepo,
    IUserPrRepository userPrRepo,
    IBodyweightRepository bodyweightRepo,
    IApplicationDbContext db,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser
) : IRequestHandler<AddExerciseSetCommand, ExerciseResponse>
{
    public async ValueTask<ExerciseResponse> Handle(AddExerciseSetCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var exercise = await exerciseRepo.FindWithSetsAndPlanAsync(request.ExerciseId, cancellationToken)
            ?? throw new InvalidOperationException("Exercise not found.");

        var plan = exercise.DailyWorkout.WeeklyWorkout.Plan;
        var canAdd = plan.PlanType == PlanType.Coach
            ? plan.CreatedByCoachId == userId
            : plan.OwnerId == userId;
        if (!canAdd)
            throw new InvalidOperationException("Only the plan owner or coach can add sets.");
        if (exercise.Sets.Count >= 100)
            throw new InvalidOperationException("An exercise cannot contain more than 100 sets.");

        var wasCompleted = exercise.IsCompleted;
        var last = exercise.Sets.OrderByDescending(s => s.SetNumber).FirstOrDefault();
        exercise.Sets.Add(new Domain.Entities.ExerciseSet
        {
            SetNumber = (last?.SetNumber ?? 0) + 1,
            PlannedReps = last?.PlannedReps ?? exercise.PlannedReps,
            PlannedWeight = last?.PlannedWeight ?? exercise.PlannedWeight,
            ExerciseId = exercise.Id,
        });
        exerciseRepo.AddSetRange(exercise.Sets.OrderByDescending(s => s.SetNumber).Take(1));
        exercise.PlannedSets = exercise.Sets.Count;
        exercise.IsCompleted = false;
        if (wasCompleted && exercise.XpAwarded)
        {
            var user = await userManager.FindByIdAsync(plan.OwnerId.ToString()) ?? throw new InvalidOperationException("User not found.");
            var removedXp = exercise.Sets.Where(s => s.IsCompleted).Sum(s => (long)Xenoh.Application.Common.XP.XpCalculator.ComputeSetXp(s.ActualWeight, s.PlannedWeight, s.ActualReps, s.PlannedReps, exercise.ExerciseTemplate.IsCompetitionLift));
            user.TotalXp = Math.Max(0, user.TotalXp - removedXp);
            user.Level = Xenoh.Application.Common.XP.XpCalculator.ComputeLevel(user.TotalXp);
            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded) throw new InvalidOperationException("Failed to update user XP.");
            exercise.XpAwarded = false;
        }
        exercise.DailyWorkout.IsCompleted = false;
        exercise.UpdatedAt = DateTime.UtcNow;
        exercise.DailyWorkout.UpdatedAt = DateTime.UtcNow;
        var hasCompletedOnDay = await db.ExerciseSets.AnyAsync(s => s.IsCompleted && s.Exercise.DailyWorkoutId == exercise.DailyWorkoutId, cancellationToken);
        if (!hasCompletedOnDay)
        {
            var history = await db.WorkoutHistories.Where(h => h.UserId == plan.OwnerId && h.Date == exercise.DailyWorkout.Date).ToListAsync(cancellationToken);
            db.WorkoutHistories.RemoveRange(history);
        }
        await exerciseRepo.SaveChangesAsync(cancellationToken);

        var pr = (await userPrRepo.GetByTemplateIdsAsync(userId, [exercise.ExerciseTemplateId], cancellationToken))
            .GetValueOrDefault(exercise.ExerciseTemplateId);
        var bodyweight = await bodyweightRepo.GetLatestWeightOnOrBeforeAsync(plan.OwnerId, exercise.DailyWorkout.Date, cancellationToken);
        return CreateExerciseHandler.ToResponse(exercise, pr, bodyweight);
    }
}
