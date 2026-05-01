using FluentAssertions;
using Xunit;
using Xenoh.Application.Features.Exercises;
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
    public void Estimate_WithoutBodyweight_ReturnsMissingBodyweight()
    {
        var result = ExerciseCalories.Estimate(9.8m, 1200, null);

        result.Calories.Should().BeNull();
        result.Status.Should().Be(ExerciseCalories.MissingBodyweight);
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
}
