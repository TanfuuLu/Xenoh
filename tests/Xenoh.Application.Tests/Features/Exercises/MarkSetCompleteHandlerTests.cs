using FluentAssertions;
using Xunit;
using Xenoh.Application.Features.Exercises.Commands.MarkSetComplete;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence.Repositories;

namespace Xenoh.Application.Tests.Features.Exercises;

public sealed class MarkSetCompleteHandlerTests : HandlerTestBase
{
    // ─── seed helpers ────────────────────────────────────────────────────────

    private async Task<(Guid SetId, Guid ExerciseTemplateId)> SeedAsync(
        bool alreadyCompleted = false,
        Guid? ownerOverride = null)
    {
        await using var ctx = CreateContext();

        var ownerId = ownerOverride ?? UserId;

        var template = new ExerciseTemplate
        {
            Name = "Back Squat",
            PrimaryMuscleGroup = MuscleGroup.Quads
        };

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

        var day = new DailyWorkout
        {
            WeeklyWorkoutId = week.Id,
            Date = DateOnly.FromDateTime(DateTime.Today),
            DayOfWeek = DateTime.Today.DayOfWeek
        };

        var exercise = new Exercise
        {
            ExerciseTemplateId = template.Id,
            Name = template.Name,
            PrimaryMuscleGroup = MuscleGroup.Quads,
            PlannedSets = 1,
            PlannedReps = 5,
            PlannedWeight = 100m,
            DailyWorkoutId = day.Id
        };

        var set = new ExerciseSet
        {
            ExerciseId = exercise.Id,
            SetNumber = 1,
            PlannedReps = 5,
            PlannedWeight = 100m,
            IsCompleted = alreadyCompleted
        };

        ctx.ExerciseTemplates.Add(template);
        ctx.Plans.Add(plan);
        ctx.WeeklyWorkouts.Add(week);
        ctx.DailyWorkouts.Add(day);
        ctx.Exercises.Add(exercise);
        ctx.ExerciseSets.Add(set);
        await ctx.SaveChangesAsync();

        return (set.Id, template.Id);
    }

    private MarkSetCompleteHandler CreateHandler(ApplicationDbContext ctx) =>
        new(
            new ExerciseSetRepository(ctx),
            new WorkoutHistoryRepository(ctx),
            new UserPrRepository(ctx),
            CurrentUser(),
            new FakeNotificationService());

    // ─── RPE tests ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithRpe_SavesRpeAndReturnsInResponse()
    {
        var (setId, _) = await SeedAsync();
        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        var command = new MarkSetCompleteCommand
        {
            SetId = setId,
            ActualReps = 5,
            ActualWeight = 100m,
            Rpe = 8.5m
        };

        var response = await handler.Handle(command, CancellationToken.None);

        var setResponse = response.Sets.Single();
        setResponse.Rpe.Should().Be(8.5m);
        setResponse.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithoutRpe_RpeRemainsNullInResponse()
    {
        var (setId, _) = await SeedAsync();
        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        var command = new MarkSetCompleteCommand
        {
            SetId = setId,
            ActualReps = 5,
            ActualWeight = 100m
            // Rpe not provided
        };

        var response = await handler.Handle(command, CancellationToken.None);

        response.Sets.Single().Rpe.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithRpe_PersistsToDatabase()
    {
        var (setId, _) = await SeedAsync();
        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        await handler.Handle(new MarkSetCompleteCommand
        {
            SetId = setId,
            ActualReps = 5,
            ActualWeight = 100m,
            Rpe = 7m
        }, CancellationToken.None);

        // Verify via separate context
        await using var verifyCtx = CreateContext();
        var persisted = await verifyCtx.ExerciseSets.FindAsync(setId);
        persisted!.Rpe.Should().Be(7m);
    }

    [Fact]
    public async Task Handle_AlreadyCompleted_ReturnsCurrentState()
    {
        var (setId, _) = await SeedAsync(alreadyCompleted: true);
        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx);

        var response = await handler.Handle(new MarkSetCompleteCommand { SetId = setId }, CancellationToken.None);

        response.Sets.Single().IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AccessDenied_ThrowsInvalidOperationException()
    {
        var otherUserId = Guid.NewGuid();
        var (setId, _) = await SeedAsync(ownerOverride: otherUserId);

        await using var ctx = CreateContext();
        var handler = CreateHandler(ctx); // CurrentUser = UserId, plan owned by otherUserId

        var act = () => handler.Handle(new MarkSetCompleteCommand { SetId = setId }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Access denied*");
    }
}
