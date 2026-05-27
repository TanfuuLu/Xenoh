using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xenoh.Application.Features.Plans.Commands.DuplicatePlan;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;
using Xenoh.Infrastructure.Services;

namespace Xenoh.Application.Tests.Features.Plans;

public sealed class DuplicatePlanHandlerTests : HandlerTestBase
{
    private DuplicatePlanHandler CreateHandler(ApplicationDbContext ctx) =>
        new(new PlanRepository(ctx), CurrentUser(), new SubscriptionService(new SubscriptionRepository(ctx)));

    [Fact]
    public async Task Handle_WhenDuplicatingOwnPlan_CreatesNewPlan()
    {
        var sourcePlanId = await SeedPlanWithExercisesAsync(UserId);
        var newStart = DateOnly.FromDateTime(DateTime.Today.AddDays(28));

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(new DuplicatePlanCommand
        {
            SourcePlanId = sourcePlanId,
            Name = "Duplicate Plan",
            StartDate = newStart,
            EndDate = newStart.AddDays(27)
        }, CancellationToken.None);

        result.Id.Should().NotBe(sourcePlanId);
        result.Name.Should().Be("Duplicate Plan");

        await using var verify = CreateContext();
        var count = await verify.Plans.CountAsync(p => p.OwnerId == UserId);
        count.Should().Be(2);
    }

    [Fact]
    public async Task Handle_WhenSourcePlanHasExercises_CopiesExercises()
    {
        var sourcePlanId = await SeedPlanWithExercisesAsync(UserId);
        var newStart = DateOnly.FromDateTime(DateTime.Today.AddDays(28));

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(new DuplicatePlanCommand
        {
            SourcePlanId = sourcePlanId,
            Name = "Duplicate",
            StartDate = newStart,
            EndDate = newStart.AddDays(27)
        }, CancellationToken.None);

        await using var verify = CreateContext();
        var exercises = await verify.Exercises
            .Where(e => e.DailyWorkout.WeeklyWorkout.PlanId == result.Id)
            .ToListAsync();
        exercises.Should().NotBeEmpty();
        exercises.Should().AllSatisfy(e => e.Name.Should().Be("Bench Press"));
    }

    [Fact]
    public async Task Handle_WhenSourcePlanNotFound_Throws()
    {
        var newStart = DateOnly.FromDateTime(DateTime.Today);

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new DuplicatePlanCommand
        {
            SourcePlanId = Guid.NewGuid(),
            Name = "Duplicate",
            StartDate = newStart,
            EndDate = newStart.AddDays(27)
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Plan not found or access denied.");
    }

    [Fact]
    public async Task Handle_WhenOtherUsersPlan_Throws()
    {
        var otherUserId = Guid.NewGuid();
        var sourcePlanId = await SeedPlanWithExercisesAsync(otherUserId);
        var newStart = DateOnly.FromDateTime(DateTime.Today);

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new DuplicatePlanCommand
        {
            SourcePlanId = sourcePlanId,
            Name = "Stolen Plan",
            StartDate = newStart,
            EndDate = newStart.AddDays(27)
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Plan not found or access denied.");
    }

    [Fact]
    public async Task Handle_WhenEndDateInvalid_Throws()
    {
        var sourcePlanId = await SeedPlanWithExercisesAsync(UserId);
        var start = DateOnly.FromDateTime(DateTime.Today.AddDays(28));

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new DuplicatePlanCommand
        {
            SourcePlanId = sourcePlanId,
            Name = "Bad Dates",
            StartDate = start,
            EndDate = start.AddDays(-1)
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("End date must be after start date.");
    }

    [Fact]
    public async Task Handle_WhenFreeUserAtPlanLimit_Throws()
    {
        var sourcePlanId = await SeedPlanWithExercisesAsync(UserId);
        for (var i = 0; i < 2; i++)
            await SeedEmptyPlanAsync(UserId);

        var newStart = DateOnly.FromDateTime(DateTime.Today.AddDays(28));

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new DuplicatePlanCommand
        {
            SourcePlanId = sourcePlanId,
            Name = "Plan 4",
            StartDate = newStart,
            EndDate = newStart.AddDays(27)
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*maximum of 3 plans*");
    }

    private async Task<Guid> SeedPlanWithExercisesAsync(Guid ownerId)
    {
        await using var ctx = CreateContext();

        ctx.Users.Add(new ApplicationUser
        {
            Id = ownerId,
            FirstName = "Test",
            LastName = "User",
            Email = $"{ownerId}@test.com",
            UserName = $"{ownerId}@test.com"
        });

        var template = new ExerciseTemplate
        {
            Name = "Bench Press",
            PrimaryMuscleGroup = MuscleGroup.Chest,
            ExerciseKind = ExerciseKind.Strength
        };

        var start = DateOnly.FromDateTime(DateTime.Today);
        var plan = new Plan
        {
            Name = "Source Plan",
            OwnerId = ownerId,
            PlanType = PlanType.Self,
            StartDate = start,
            EndDate = start.AddDays(6)
        };

        var week = new WeeklyWorkout
        {
            PlanId = plan.Id,
            WeekNumber = 1,
            Name = "Week 1",
            StartDate = start,
            EndDate = start.AddDays(6)
        };

        var day = new DailyWorkout
        {
            WeeklyWorkoutId = week.Id,
            Date = start,
            DayOfWeek = start.DayOfWeek
        };

        var exercise = new Exercise
        {
            DailyWorkoutId = day.Id,
            ExerciseTemplateId = template.Id,
            Name = "Bench Press",
            PrimaryMuscleGroup = MuscleGroup.Chest,
            ExerciseKind = ExerciseKind.Strength,
            PlannedSets = 3,
            PlannedReps = 10,
            Sets = Enumerable.Range(1, 3).Select(i => new ExerciseSet { SetNumber = i, PlannedReps = 10 }).ToList<ExerciseSet>()
        };

        ctx.ExerciseTemplates.Add(template);
        ctx.Plans.Add(plan);
        ctx.WeeklyWorkouts.Add(week);
        ctx.DailyWorkouts.Add(day);
        ctx.Exercises.Add(exercise);
        await ctx.SaveChangesAsync();

        return plan.Id;
    }

    private async Task SeedEmptyPlanAsync(Guid ownerId)
    {
        await using var ctx = CreateContext();
        ctx.Plans.Add(new Plan
        {
            Name = "Extra Plan",
            OwnerId = ownerId,
            PlanType = PlanType.Self,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(27))
        });
        await ctx.SaveChangesAsync();
    }
}
