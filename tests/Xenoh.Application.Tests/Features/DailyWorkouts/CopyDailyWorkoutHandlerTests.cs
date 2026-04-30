using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xenoh.Application.Features.DailyWorkouts.Commands.CopyDailyWorkout;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence.Repositories;

namespace Xenoh.Application.Tests.Features.DailyWorkouts;

public sealed class CopyDailyWorkoutHandlerTests : HandlerTestBase
{
    // ─── seed helpers ────────────────────────────────────────────────────────

    private record SeededDays(Guid SourceId, Guid TargetId);

    private CopyDailyWorkoutHandler CreateHandler(Xenoh.Infrastructure.Persistence.ApplicationDbContext ctx) =>
        new(new DailyWorkoutRepository(ctx), new ExerciseRepository(ctx), CurrentUser());

    /// <summary>
    /// Creates a plan with two daily workouts under the same week.
    /// Source gets <paramref name="sourceExerciseCount"/> exercises, each with <paramref name="setsPerExercise"/> sets.
    /// Target gets <paramref name="targetExerciseCount"/> exercises (pre-existing, to test replacement).
    /// </summary>
    private async Task<SeededDays> SeedAsync(
        int sourceExerciseCount = 2,
        int setsPerExercise = 3,
        int targetExerciseCount = 0,
        Guid? ownerOverride = null)
    {
        await using var ctx = CreateContext();

        var ownerId = ownerOverride ?? UserId;

        var plan = new Plan
        {
            Name = "Test Plan",
            OwnerId = ownerId,
            PlanType = PlanType.Self,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30))
        };

        var week = new WeeklyWorkout
        {
            PlanId = plan.Id,
            WeekNumber = 1,
            Name = "Week 1",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(6))
        };

        var source = new DailyWorkout
        {
            WeeklyWorkoutId = week.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            DayOfWeek = DayOfWeek.Monday
        };

        var target = new DailyWorkout
        {
            WeeklyWorkoutId = week.Id,
            Date = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            DayOfWeek = DayOfWeek.Tuesday
        };

        ctx.Plans.Add(plan);
        ctx.WeeklyWorkouts.Add(week);
        ctx.DailyWorkouts.Add(source);
        ctx.DailyWorkouts.Add(target);

        // Seed source exercises
        for (int i = 0; i < sourceExerciseCount; i++)
        {
            var template = new ExerciseTemplate
            {
                Name = $"Exercise {i + 1}",
                PrimaryMuscleGroup = MuscleGroup.Chest
            };
            ctx.ExerciseTemplates.Add(template);

            var exercise = new Exercise
            {
                ExerciseTemplateId = template.Id,
                Name = template.Name,
                PrimaryMuscleGroup = MuscleGroup.Chest,
                PlannedSets = setsPerExercise,
                PlannedReps = 10,
                PlannedWeight = 60m + i * 10,
                DailyWorkoutId = source.Id
            };
            ctx.Exercises.Add(exercise);

            for (int s = 1; s <= setsPerExercise; s++)
            {
                ctx.ExerciseSets.Add(new ExerciseSet
                {
                    ExerciseId = exercise.Id,
                    SetNumber = s,
                    PlannedReps = 10,
                    PlannedWeight = 60m + i * 10
                });
            }
        }

        // Seed pre-existing target exercises
        for (int i = 0; i < targetExerciseCount; i++)
        {
            var t = new ExerciseTemplate { Name = $"Old Exercise {i}", PrimaryMuscleGroup = MuscleGroup.Back };
            ctx.ExerciseTemplates.Add(t);

            var ex = new Exercise
            {
                ExerciseTemplateId = t.Id,
                Name = t.Name,
                PrimaryMuscleGroup = MuscleGroup.Back,
                PlannedSets = 1,
                PlannedReps = 5,
                DailyWorkoutId = target.Id
            };
            ctx.Exercises.Add(ex);
        }

        await ctx.SaveChangesAsync();
        return new SeededDays(source.Id, target.Id);
    }

    // ─── tests ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidCopy_ClonesExercisesAndSets()
    {
        var days = await SeedAsync(sourceExerciseCount: 2, setsPerExercise: 3);

        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        var response = await handler.Handle(
            new CopyDailyWorkoutCommand(days.SourceId, days.TargetId),
            CancellationToken.None);

        response.ExercisesCopied.Should().Be(2);
        response.TargetDailyWorkoutId.Should().Be(days.TargetId);

        await using var verifyCtx = CreateContext();
        var exercises = await verifyCtx.Exercises
            .Include(e => e.Sets)
            .Where(e => e.DailyWorkoutId == days.TargetId)
            .ToListAsync();

        exercises.Should().HaveCount(2);
        exercises.Should().AllSatisfy(e =>
        {
            e.Sets.Should().HaveCount(3);
            e.IsCompleted.Should().BeFalse();
            e.Sets.Should().AllSatisfy(s =>
            {
                s.IsCompleted.Should().BeFalse();
                s.ActualReps.Should().BeNull();
                s.ActualWeight.Should().BeNull();
                s.Rpe.Should().BeNull();
            });
        });
    }

    [Fact]
    public async Task Handle_TargetHasExistingExercises_ReplacesAll()
    {
        // Target starts with 1 existing exercise; source has 2 — result should be 2, not 3
        var days = await SeedAsync(sourceExerciseCount: 2, targetExerciseCount: 1);

        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        await handler.Handle(
            new CopyDailyWorkoutCommand(days.SourceId, days.TargetId),
            CancellationToken.None);

        await using var verifyCtx = CreateContext();
        var count = await verifyCtx.Exercises.CountAsync(e => e.DailyWorkoutId == days.TargetId);
        count.Should().Be(2);
    }

    [Fact]
    public async Task Handle_PlannedDataCopied_ActualsNotCarriedOver()
    {
        var days = await SeedAsync(sourceExerciseCount: 1, setsPerExercise: 2);

        // Mark source sets as completed with actuals
        await using var completeCtx = CreateContext();
        var sourceSets = await completeCtx.ExerciseSets
            .Where(s => s.Exercise.DailyWorkoutId == days.SourceId)
            .ToListAsync();
        foreach (var s in sourceSets)
        {
            s.IsCompleted = true;
            s.ActualReps = 8;
            s.ActualWeight = 70m;
            s.Rpe = 9m;
        }
        await completeCtx.SaveChangesAsync();

        // Now copy
        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);
        await handler.Handle(new CopyDailyWorkoutCommand(days.SourceId, days.TargetId), CancellationToken.None);

        // Cloned sets should have planned data only, no actuals
        await using var verifyCtx = CreateContext();
        var targetSets = await verifyCtx.ExerciseSets
            .Where(s => s.Exercise.DailyWorkoutId == days.TargetId)
            .ToListAsync();

        targetSets.Should().AllSatisfy(s =>
        {
            s.IsCompleted.Should().BeFalse();
            s.ActualReps.Should().BeNull();
            s.ActualWeight.Should().BeNull();
            s.Rpe.Should().BeNull();
            s.PlannedReps.Should().Be(10);
        });
    }

    [Fact]
    public async Task Handle_SameSourceAndTarget_Throws()
    {
        var days = await SeedAsync();

        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        var act = () => handler.Handle(
            new CopyDailyWorkoutCommand(days.SourceId, days.SourceId),
            CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*same daily workout*");
    }

    [Fact]
    public async Task Handle_SourceNotFound_Throws()
    {
        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        var act = () => handler.Handle(
            new CopyDailyWorkoutCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Source daily workout not found*");
    }

    [Fact]
    public async Task Handle_AccessDeniedOnSource_Throws()
    {
        var otherUser = Guid.NewGuid();
        var days = await SeedAsync(ownerOverride: otherUser); // plan owned by someone else

        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx); // CurrentUser != otherUser

        var act = () => handler.Handle(
            new CopyDailyWorkoutCommand(days.SourceId, days.TargetId),
            CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Access denied*");
    }
}
