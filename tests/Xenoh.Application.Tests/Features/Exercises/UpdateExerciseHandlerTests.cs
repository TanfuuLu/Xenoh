using FluentAssertions;
using Xunit;
using Xenoh.Application.Features.Exercises.Commands.UpdateExercise;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;

namespace Xenoh.Application.Tests.Features.Exercises;

public sealed class UpdateExerciseHandlerTests : HandlerTestBase
{
    private UpdateExerciseHandler CreateHandler(ApplicationDbContext ctx) =>
        new(new ExerciseRepository(ctx), CurrentUser());

    [Fact]
    public async Task Handle_WhenPlannedWeightChanges_UpdatesIncompleteSetPlannedWeights()
    {
        var exerciseId = await SeedExerciseAsync();

        await using var ctx = CreateContext();
        var response = await CreateHandler(ctx).Handle(
            new UpdateExerciseCommand
            {
                ExerciseId = exerciseId,
                PlannedWeight = 40m
            },
            CancellationToken.None);

        response.PlannedWeight.Should().Be(40m);
        response.Sets.Should().OnlyContain(s => s.PlannedWeight == 40m);

        await using var verify = CreateContext();
        var exercise = verify.Exercises.Single(e => e.Id == exerciseId);
        exercise.PlannedWeight.Should().Be(40m);
        verify.ExerciseSets
            .Where(s => s.ExerciseId == exerciseId)
            .Should()
            .OnlyContain(s => s.PlannedWeight == 40m);
    }

    [Fact]
    public async Task Handle_WhenSetCompleted_DoesNotChangeCompletedSetPlannedWeight()
    {
        var exerciseId = await SeedExerciseAsync(completedFirstSet: true);

        await using var ctx = CreateContext();
        var response = await CreateHandler(ctx).Handle(
            new UpdateExerciseCommand
            {
                ExerciseId = exerciseId,
                PlannedWeight = 40m
            },
            CancellationToken.None);

        response.PlannedWeight.Should().Be(40m);
        response.Sets.Single(s => s.SetNumber == 1).PlannedWeight.Should().Be(47.5m);
        response.Sets.Single(s => s.SetNumber == 2).PlannedWeight.Should().Be(40m);
    }

    [Fact]
    public async Task Handle_WhenPlannedSetsReduced_RemovesSurplusSetsAndRenumbers()
    {
        var exerciseId = await SeedExerciseAsync(); // seeds 2 sets

        await using var ctx = CreateContext();
        var response = await CreateHandler(ctx).Handle(
            new UpdateExerciseCommand
            {
                ExerciseId = exerciseId,
                PlannedSets = 1
            },
            CancellationToken.None);

        response.PlannedSets.Should().Be(1);
        response.Sets.Should().HaveCount(1);
        response.Sets.Single().SetNumber.Should().Be(1);

        await using var verify = CreateContext();
        verify.ExerciseSets.Where(s => s.ExerciseId == exerciseId).Should().HaveCount(1);
        verify.Exercises.Single(e => e.Id == exerciseId).PlannedSets.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenPlannedSetsIncreased_AddsMissingSetsWithContiguousNumbers()
    {
        var exerciseId = await SeedExerciseAsync(); // seeds 2 sets

        await using var ctx = CreateContext();
        var response = await CreateHandler(ctx).Handle(
            new UpdateExerciseCommand
            {
                ExerciseId = exerciseId,
                PlannedSets = 4,
                PlannedWeight = 50m
            },
            CancellationToken.None);

        response.PlannedSets.Should().Be(4);
        response.Sets.Should().HaveCount(4);
        response.Sets.Select(s => s.SetNumber).Should().BeEquivalentTo(new[] { 1, 2, 3, 4 });
        response.Sets.Should().OnlyContain(s => s.PlannedWeight == 50m);

        await using var verify = CreateContext();
        verify.ExerciseSets.Where(s => s.ExerciseId == exerciseId).Should().HaveCount(4);
    }

    private async Task<Guid> SeedExerciseAsync(bool completedFirstSet = false)
    {
        await using var ctx = CreateContext();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var template = new ExerciseTemplate
        {
            Name = "Close-Grip Bench Press",
            PrimaryMuscleGroup = MuscleGroup.Triceps
        };

        var plan = new Plan
        {
            Name = "Test Plan",
            OwnerId = UserId,
            PlanType = PlanType.Self,
            StartDate = today,
            EndDate = today.AddDays(6)
        };

        var week = new WeeklyWorkout
        {
            PlanId = plan.Id,
            WeekNumber = 1,
            Name = "Week 1",
            StartDate = today,
            EndDate = today.AddDays(6)
        };

        var day = new DailyWorkout
        {
            WeeklyWorkoutId = week.Id,
            Date = today,
            DayOfWeek = today.DayOfWeek
        };

        var exercise = new Exercise
        {
            ExerciseTemplateId = template.Id,
            Name = template.Name,
            PrimaryMuscleGroup = MuscleGroup.Triceps,
            PlannedSets = 2,
            PlannedReps = 6,
            PlannedWeight = 47.5m,
            DailyWorkoutId = day.Id
        };

        exercise.Sets.Add(new ExerciseSet
        {
            SetNumber = 1,
            PlannedReps = 6,
            PlannedWeight = 47.5m,
            IsCompleted = completedFirstSet
        });
        exercise.Sets.Add(new ExerciseSet
        {
            SetNumber = 2,
            PlannedReps = 6,
            PlannedWeight = 47.5m
        });

        ctx.ExerciseTemplates.Add(template);
        ctx.Plans.Add(plan);
        ctx.WeeklyWorkouts.Add(week);
        ctx.DailyWorkouts.Add(day);
        ctx.Exercises.Add(exercise);
        await ctx.SaveChangesAsync();

        return exercise.Id;
    }
}
