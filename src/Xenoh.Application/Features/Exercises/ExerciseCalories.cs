using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Exercises;

public static class ExerciseCalories
{
    public const string Ready = "Ready";
    public const string MissingDuration = "MissingDuration";
    public const string MissingBodyweight = "MissingBodyweight";

    private const decimal SecondsPerHour = 3600m;
    private const decimal TempoSecondsPerRep = 4m;
    private const decimal RestMet = 1.5m;
    private const decimal CompoundStrengthMet = 6.0m;
    private const decimal IsolationStrengthMet = 3.5m;

    private static readonly string[] IsolationKeywords =
    [
        "curl",
        "raise",
        "extension",
        "pushdown",
        "pressdown",
        "fly",
        "shrug",
        "kickback",
        "calf raise"
    ];

    private static readonly string[] CompoundKeywords =
    [
        "squat",
        "deadlift",
        "bench",
        "press",
        "row",
        "pull up",
        "pull-up",
        "pulldown",
        "dip",
        "lunge",
        "clean",
        "snatch",
        "thruster",
        "hip thrust"
    ];

    public static (decimal? Calories, string Status) Estimate(
        decimal estimatedMet,
        int? durationSeconds,
        decimal? bodyweightKg)
    {
        return EstimateCardio(estimatedMet, durationSeconds, bodyweightKg);
    }

    public static (decimal? Calories, string Status) Estimate(
        ExerciseKind exerciseKind,
        string exerciseName,
        decimal estimatedMet,
        int? durationSeconds,
        decimal? bodyweightKg,
        IEnumerable<ExerciseSet> sets,
        bool isCompetitionLift,
        IEnumerable<MuscleGroup> secondaryMuscleGroups)
    {
        if (durationSeconds is null or <= 0)
            return (null, MissingDuration);

        if (bodyweightKg is null)
            return (null, MissingBodyweight);

        if (exerciseKind == ExerciseKind.Cardio)
            return EstimateCardio(estimatedMet, durationSeconds, bodyweightKg);

        var completedReps = sets
            .Where(s => s.IsCompleted)
            .Sum(s => s.ActualReps ?? s.PlannedReps);

        var activeSeconds = Math.Min((decimal)durationSeconds.Value, completedReps * TempoSecondsPerRep);
        var restSeconds = Math.Max(0m, durationSeconds.Value - activeSeconds);
        var activeMet = InferStrengthMet(exerciseName, isCompetitionLift, secondaryMuscleGroups);

        var calories = bodyweightKg.Value *
            ((activeSeconds / SecondsPerHour * activeMet) + (restSeconds / SecondsPerHour * RestMet));

        return (decimal.Round(calories, 0, MidpointRounding.AwayFromZero), Ready);
    }

    private static (decimal? Calories, string Status) EstimateCardio(
        decimal estimatedMet,
        int? durationSeconds,
        decimal? bodyweightKg)
    {
        if (durationSeconds is null or <= 0)
            return (null, MissingDuration);

        if (bodyweightKg is null)
            return (null, MissingBodyweight);

        var durationHours = (decimal)durationSeconds.Value / SecondsPerHour;
        var calories = estimatedMet * bodyweightKg.Value * durationHours;

        return (decimal.Round(calories, 0, MidpointRounding.AwayFromZero), Ready);
    }

    private static decimal InferStrengthMet(
        string exerciseName,
        bool isCompetitionLift,
        IEnumerable<MuscleGroup> secondaryMuscleGroups)
    {
        if (ContainsAny(exerciseName, IsolationKeywords))
            return IsolationStrengthMet;

        if (isCompetitionLift || ContainsAny(exerciseName, CompoundKeywords))
            return CompoundStrengthMet;

        var hasMeaningfulSecondaryMuscles = secondaryMuscleGroups.Any(g => g != MuscleGroup.Cardio);
        return hasMeaningfulSecondaryMuscles ? CompoundStrengthMet : IsolationStrengthMet;
    }

    private static bool ContainsAny(string value, IEnumerable<string> keywords) =>
        keywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
}
