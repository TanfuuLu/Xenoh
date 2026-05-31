using FluentAssertions;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Xenoh.Application.Tests.Features.Users;

public sealed class TrainingActivityRepositoryTests : HandlerTestBase
{
    private readonly DateOnly _accountCreatedDate = new(2026, 5, 3);
    private readonly DateOnly _today = new(2026, 5, 29);

    [Fact]
    public async Task GetActivityAsync_SumsOnlyCurrentUserPositiveDurationsWithinAccountLifetime()
    {
        await using var seedCtx = CreateContext();
        await SeedExerciseAsync(seedCtx, UserId, new DateOnly(2026, 5, 4), 1800);
        await SeedExerciseAsync(seedCtx, UserId, new DateOnly(2026, 5, 5), 0);
        await SeedExerciseAsync(seedCtx, UserId, new DateOnly(2026, 5, 6), null);
        await SeedExerciseAsync(seedCtx, UserId, new DateOnly(2026, 5, 7), 1200);
        await SeedExerciseAsync(seedCtx, UserId, new DateOnly(2026, 5, 30), 900);
        await SeedExerciseAsync(seedCtx, UserId, new DateOnly(2026, 5, 2), 600);
        await SeedExerciseAsync(seedCtx, Guid.NewGuid(), new DateOnly(2026, 5, 8), 9999);
        await seedCtx.SaveChangesAsync();

        await using var ctx = CreateContext();
        var result = await new TrainingActivityRepository(ctx).GetActivityAsync(
            UserId,
            _accountCreatedDate,
            _today,
            2026,
            5,
            CancellationToken.None);

        result.TotalDurationSeconds.Should().Be(3000);
    }

    [Fact]
    public async Task GetActivityAsync_SumsOnlyCompletedSetVolumeWithinAccountLifetime()
    {
        await using var seedCtx = CreateContext();
        var exercise = await SeedExerciseAsync(seedCtx, UserId, new DateOnly(2026, 5, 4), 1800);
        var beforeAccount = await SeedExerciseAsync(seedCtx, UserId, new DateOnly(2026, 5, 2), 1800);
        var afterToday = await SeedExerciseAsync(seedCtx, UserId, new DateOnly(2026, 5, 30), 1800);
        var otherUser = await SeedExerciseAsync(seedCtx, Guid.NewGuid(), new DateOnly(2026, 5, 4), 1800);

        seedCtx.ExerciseSets.AddRange(
            new ExerciseSet
            {
                ExerciseId = exercise.Id,
                SetNumber = 1,
                PlannedReps = 5,
                PlannedWeight = 100m,
                IsCompleted = true
            },
            new ExerciseSet
            {
                ExerciseId = exercise.Id,
                SetNumber = 2,
                PlannedReps = 5,
                PlannedWeight = 100m,
                ActualReps = 4,
                ActualWeight = 105m,
                IsCompleted = true
            },
            new ExerciseSet
            {
                ExerciseId = exercise.Id,
                SetNumber = 3,
                PlannedReps = 5,
                PlannedWeight = 100m,
                IsCompleted = false
            },
            new ExerciseSet
            {
                ExerciseId = beforeAccount.Id,
                SetNumber = 1,
                PlannedReps = 10,
                PlannedWeight = 100m,
                IsCompleted = true
            },
            new ExerciseSet
            {
                ExerciseId = afterToday.Id,
                SetNumber = 1,
                PlannedReps = 10,
                PlannedWeight = 100m,
                IsCompleted = true
            },
            new ExerciseSet
            {
                ExerciseId = otherUser.Id,
                SetNumber = 1,
                PlannedReps = 10,
                PlannedWeight = 100m,
                IsCompleted = true
            });
        await seedCtx.SaveChangesAsync();

        await using var ctx = CreateContext();
        var result = await new TrainingActivityRepository(ctx).GetActivityAsync(
            UserId,
            _accountCreatedDate,
            _today,
            2026,
            5,
            CancellationToken.None);

        result.TotalWeightTrainedKg.Should().Be(920m);
    }

    [Fact]
    public async Task GetActivityAsync_ReturnsRequestedMonthDatesAndMergesSourcesWithoutDuplicates()
    {
        await using var seedCtx = CreateContext();
        await SeedExerciseAsync(seedCtx, UserId, new DateOnly(2026, 5, 4), 1800);
        await SeedExerciseAsync(seedCtx, UserId, new DateOnly(2026, 5, 6), 900);
        await SeedExerciseAsync(seedCtx, UserId, new DateOnly(2026, 6, 1), 900);
        await SeedExerciseAsync(seedCtx, UserId, new DateOnly(2026, 5, 7), 0);
        await SeedExerciseAsync(seedCtx, Guid.NewGuid(), new DateOnly(2026, 5, 8), 1200);

        seedCtx.WorkoutHistories.AddRange(
            new WorkoutHistory { UserId = UserId, Date = new DateOnly(2026, 5, 4) },
            new WorkoutHistory { UserId = UserId, Date = new DateOnly(2026, 5, 10) },
            new WorkoutHistory { UserId = UserId, Date = new DateOnly(2026, 4, 30) },
            new WorkoutHistory { UserId = UserId, Date = new DateOnly(2026, 5, 30) },
            new WorkoutHistory { UserId = Guid.NewGuid(), Date = new DateOnly(2026, 5, 11) });
        await seedCtx.SaveChangesAsync();

        await using var ctx = CreateContext();
        var result = await new TrainingActivityRepository(ctx).GetActivityAsync(
            UserId,
            _accountCreatedDate,
            _today,
            2026,
            5,
            CancellationToken.None);

        result.TrainedDates.Should().Equal(
            new DateOnly(2026, 5, 4),
            new DateOnly(2026, 5, 6),
            new DateOnly(2026, 5, 10));
    }

    [Fact]
    public async Task GetActivityAsync_ReturnsNoCalendarDatesOutsideEffectiveRange()
    {
        await using var seedCtx = CreateContext();
        await SeedExerciseAsync(seedCtx, UserId, new DateOnly(2026, 6, 1), 1800);
        seedCtx.WorkoutHistories.Add(new WorkoutHistory { UserId = UserId, Date = new DateOnly(2026, 6, 2) });
        await seedCtx.SaveChangesAsync();

        await using var ctx = CreateContext();
        var result = await new TrainingActivityRepository(ctx).GetActivityAsync(
            UserId,
            _accountCreatedDate,
            _today,
            2026,
            6,
            CancellationToken.None);

        result.TrainedDates.Should().BeEmpty();
        result.TotalDurationSeconds.Should().Be(0);
    }

    private static async Task<Exercise> SeedExerciseAsync(
        ApplicationDbContext ctx,
        Guid ownerId,
        DateOnly date,
        int? durationSeconds)
    {
        var template = new ExerciseTemplate
        {
            Name = $"Template {Guid.NewGuid():N}",
            PrimaryMuscleGroup = MuscleGroup.Chest
        };

        var plan = new Plan
        {
            Name = $"Plan {Guid.NewGuid():N}",
            OwnerId = ownerId,
            PlanType = PlanType.Self,
            StartDate = date,
            EndDate = date
        };

        var week = new WeeklyWorkout
        {
            PlanId = plan.Id,
            WeekNumber = 1,
            Name = "Week 1",
            StartDate = date,
            EndDate = date
        };

        var day = new DailyWorkout
        {
            WeeklyWorkoutId = week.Id,
            Date = date,
            DayOfWeek = date.DayOfWeek
        };

        var exercise = new Exercise
        {
            ExerciseTemplateId = template.Id,
            Name = template.Name,
            PrimaryMuscleGroup = MuscleGroup.Chest,
            PlannedSets = 1,
            PlannedReps = 10,
            DailyWorkoutId = day.Id,
            DurationSeconds = durationSeconds
        };

        ctx.ExerciseTemplates.Add(template);
        ctx.Plans.Add(plan);
        ctx.WeeklyWorkouts.Add(week);
        ctx.DailyWorkouts.Add(day);
        ctx.Exercises.Add(exercise);
        await Task.CompletedTask;

        return exercise;
    }
}
