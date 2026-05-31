using FluentAssertions;
using Xunit;
using Xenoh.Application.Features.Exercises;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence.Seeders;

namespace Xenoh.Application.Tests.Features.Exercises;

public sealed class ExerciseCaloriesTests
{
    [Fact]
    public void Estimate_WithBodyweightAndDuration_ReturnsCalories()
    {
        var result = ExerciseCalories.Estimate(10m, 1800, 80m);

        result.Calories.Should().Be(400m);
        result.Status.Should().Be(ExerciseCalories.Ready);
    }

    [Fact]
    public void Estimate_ForCardio_UsesMetAcrossFullDuration()
    {
        var result = ExerciseCalories.Estimate(
            ExerciseKind.Cardio,
            "Running",
            10m,
            1800,
            80m,
            [],
            false,
            [MuscleGroup.Quads]);

        result.Calories.Should().Be(400m);
        result.Status.Should().Be(ExerciseCalories.Ready);
    }

    [Fact]
    public void Estimate_ForCompoundStrength_UsesActiveAndRestSplit()
    {
        var sets = new[]
        {
            CompletedSet(10),
            CompletedSet(15)
        };

        var result = ExerciseCalories.Estimate(
            ExerciseKind.Strength,
            "Bench Press",
            5m,
            600,
            80m,
            sets,
            false,
            [MuscleGroup.Triceps]);

        result.Calories.Should().Be(30m);
        result.Status.Should().Be(ExerciseCalories.Ready);
    }

    [Fact]
    public void Estimate_ForIsolationStrength_UsesLowerActiveMet()
    {
        var sets = new[]
        {
            CompletedSet(10),
            CompletedSet(15)
        };

        var result = ExerciseCalories.Estimate(
            ExerciseKind.Strength,
            "Biceps Curl",
            5m,
            600,
            80m,
            sets,
            false,
            [MuscleGroup.Forearms]);

        result.Calories.Should().Be(24m);
        result.Status.Should().Be(ExerciseCalories.Ready);
    }

    [Fact]
    public void Estimate_ForStrength_CapsActiveSecondsAtTotalDuration()
    {
        var result = ExerciseCalories.Estimate(
            ExerciseKind.Strength,
            "Squat",
            5m,
            20,
            80m,
            [CompletedSet(10)],
            false,
            [MuscleGroup.Glutes]);

        result.Calories.Should().Be(3m);
        result.Status.Should().Be(ExerciseCalories.Ready);
    }

    [Fact]
    public void Estimate_WithoutBodyweight_ReturnsMissingBodyweight()
    {
        var result = ExerciseCalories.Estimate(9.8m, 1200, null);

        result.Calories.Should().BeNull();
        result.Status.Should().Be(ExerciseCalories.MissingBodyweight);
    }

    [Fact]
    public void Estimate_WithoutDuration_ReturnsMissingDuration()
    {
        var result = ExerciseCalories.Estimate(
            ExerciseKind.Strength,
            "Squat",
            5m,
            null,
            80m,
            [CompletedSet(5)],
            true,
            [MuscleGroup.Glutes]);

        result.Calories.Should().BeNull();
        result.Status.Should().Be(ExerciseCalories.MissingDuration);
    }

    [Fact]
    public void SeededCardioTemplates_UseCardioKindAndHigherMetValues()
    {
        var templates = ExerciseTemplateSeeder.GetTemplates();

        templates.Single(t => t.Name == "Running").ExerciseKind.Should().Be(ExerciseKind.Cardio);
        templates.Single(t => t.Name == "Running").EstimatedMet.Should().Be(9.8m);
        templates.Single(t => t.Name == "Jump Rope").EstimatedMet.Should().Be(10.0m);
        templates.Single(t => t.Name == "Bench Press").ExerciseKind.Should().Be(ExerciseKind.Strength);
        templates.Single(t => t.Name == "Bench Press").EstimatedMet.Should().Be(5.0m);
    }

    private static ExerciseSet CompletedSet(int reps) =>
        new()
        {
            PlannedReps = reps,
            IsCompleted = true
        };
}
