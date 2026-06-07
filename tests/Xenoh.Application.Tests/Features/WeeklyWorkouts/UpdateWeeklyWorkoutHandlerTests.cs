using FluentAssertions;
using Xunit;
using Xenoh.Application.Features.WeeklyWorkouts.Commands.UpdateWeeklyWorkout;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;

namespace Xenoh.Application.Tests.Features.WeeklyWorkouts;

public sealed class UpdateWeeklyWorkoutHandlerTests : HandlerTestBase
{
    private UpdateWeeklyWorkoutHandler CreateHandler(ApplicationDbContext ctx) =>
        new(new WeeklyWorkoutRepository(ctx), CurrentUser());

    [Fact]
    public async Task Handle_WhenOwnerUpdatesName_PersistsChange()
    {
        var weekId = await SeedWeekAsync(UserId);

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(new UpdateWeeklyWorkoutCommand
        {
            WeeklyWorkoutId = weekId,
            Name = "Renamed Week"
        }, CancellationToken.None);

        result.Name.Should().Be("Renamed Week");

        await using var verify = CreateContext();
        var week = await verify.WeeklyWorkouts.FindAsync(weekId);
        week!.Name.Should().Be("Renamed Week");
    }

    [Fact]
    public async Task Handle_WhenNonOwner_Throws()
    {
        var weekId = await SeedWeekAsync(Guid.NewGuid()); // owned by someone else

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new UpdateWeeklyWorkoutCommand
        {
            WeeklyWorkoutId = weekId,
            Name = "New Name"
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Access denied.");
    }

    [Fact]
    public async Task Handle_WhenWeekNotFound_Throws()
    {
        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new UpdateWeeklyWorkoutCommand
        {
            WeeklyWorkoutId = Guid.NewGuid(),
            Name = "Name"
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Weekly workout not found.");
    }

    [Fact]
    public async Task Handle_WhenClientTriesCoachPlan_Throws()
    {
        var coachId = Guid.NewGuid();
        var weekId = await SeedCoachPlanWeekAsync(clientId: UserId, coachId: coachId);

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new UpdateWeeklyWorkoutCommand
        {
            WeeklyWorkoutId = weekId,
            Name = "New Name"
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*managed by your coach*");
    }

    [Fact]
    public async Task Handle_WhenCoachEditsCoachPlan_Succeeds()
    {
        var weekId = await SeedCoachPlanWeekAsync(clientId: Guid.NewGuid(), coachId: UserId);

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(new UpdateWeeklyWorkoutCommand
        {
            WeeklyWorkoutId = weekId,
            Name = "Coach Edited"
        }, CancellationToken.None);

        result.Name.Should().Be("Coach Edited");
    }

    private async Task<Guid> SeedWeekAsync(Guid ownerId)
    {
        await using var ctx = CreateContext();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var plan = new Plan
        {
            Name = "Test Plan",
            OwnerId = ownerId,
            PlanType = PlanType.Self,
            StartDate = today,
            EndDate = today.AddDays(27)
        };
        var week = new WeeklyWorkout
        {
            PlanId = plan.Id,
            WeekNumber = 1,
            Name = "Week 1",
            StartDate = today,
            EndDate = today.AddDays(6)
        };
        ctx.Plans.Add(plan);
        ctx.WeeklyWorkouts.Add(week);
        await ctx.SaveChangesAsync();
        return week.Id;
    }

    private async Task<Guid> SeedCoachPlanWeekAsync(Guid clientId, Guid coachId)
    {
        await using var ctx = CreateContext();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var plan = new Plan
        {
            Name = "Coach Plan",
            OwnerId = clientId,
            CreatedByCoachId = coachId,
            PlanType = PlanType.Coach,
            StartDate = today,
            EndDate = today.AddDays(27)
        };
        var week = new WeeklyWorkout
        {
            PlanId = plan.Id,
            WeekNumber = 1,
            Name = "Week 1",
            StartDate = today,
            EndDate = today.AddDays(6)
        };
        ctx.Plans.Add(plan);
        ctx.WeeklyWorkouts.Add(week);
        await ctx.SaveChangesAsync();
        return week.Id;
    }
}
