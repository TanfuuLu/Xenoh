using Microsoft.AspNetCore.Identity;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.XP;

public static class ExerciseXpAwarder
{
    public const int ValidExerciseDurationSeconds = 60;

    public static bool IsValidExercise(Exercise exercise) =>
        exercise.DurationSeconds is > ValidExerciseDurationSeconds;

    public static int ComputeExerciseXp(Exercise exercise)
    {
        if (!IsValidExercise(exercise))
            return 0;

        return exercise.Sets
            .Where(set => set.IsCompleted)
            .Sum(set => XpCalculator.ComputeSetXp(
                set.ActualWeight,
                set.PlannedWeight,
                set.ActualReps,
                set.PlannedReps,
                exercise.ExerciseTemplate.IsCompetitionLift));
    }

    public static async Task AwardIfEligibleAsync(
        UserManager<ApplicationUser> userManager,
        Guid userId,
        Exercise exercise)
    {
        if (exercise.XpAwarded || !exercise.IsCompleted || !IsValidExercise(exercise))
            return;

        var xpEarned = ComputeExerciseXp(exercise);
        if (xpEarned <= 0)
            return;

        var xpUser = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        xpUser.TotalXp += xpEarned;
        xpUser.Level = XpCalculator.ComputeLevel(xpUser.TotalXp);

        var result = await userManager.UpdateAsync(xpUser);
        if (!result.Succeeded)
            throw new InvalidOperationException("Failed to update user XP.");

        exercise.XpAwarded = true;
    }
}
