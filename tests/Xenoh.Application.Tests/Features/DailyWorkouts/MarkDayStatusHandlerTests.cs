using FluentAssertions;
using Xunit;
using Xenoh.Application.Features.DailyWorkouts.Commands.MarkDayStatus;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;

namespace Xenoh.Application.Tests.Features.DailyWorkouts;

public sealed class MarkDayStatusHandlerTests : HandlerTestBase
{
    private MarkDayStatusHandler CreateHandler(ApplicationDbContext ctx) =>
        new(new DailyWorkoutRepository(ctx), CurrentUser());

    [Fact]
    public async Task Handle_WhenOwnerMarksRest_UpdatesStatus()
    {
        var dayId = await SeedDayAsync(UserId);

        await using var ctx = CreateContext();
        await CreateHandler(ctx).Handle(new MarkDayStatusCommand(dayId, DayStatus.Rest), CancellationToken.None);

        await using var verify = CreateContext();
        var day = await verify.DailyWorkouts.FindAsync(dayId);
        day!.Status.Should().Be(DayStatus.Rest);
    }

    [Fact]
    public async Task Handle_WhenOwnerMarksMissed_UpdatesStatus()
    {
        var dayId = await SeedDayAsync(UserId);

        await using var ctx = CreateContext();
        await CreateHandler(ctx).Handle(new MarkDayStatusCommand(dayId, DayStatus.Missed), CancellationToken.None);

        await using var verify = CreateContext();
        var day = await verify.DailyWorkouts.FindAsync(dayId);
        day!.Status.Should().Be(DayStatus.Missed);
    }

    [Fact]
    public async Task Handle_WhenNonOwner_Throws()
    {
        var otherUserId = Guid.NewGuid();
        var dayId = await SeedDayAsync(otherUserId);

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new MarkDayStatusCommand(dayId, DayStatus.Rest), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*plan owner*");
    }

    [Fact]
    public async Task Handle_WhenDayNotFound_Throws()
    {
        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new MarkDayStatusCommand(Guid.NewGuid(), DayStatus.Rest), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Daily workout not found.");
    }

    private async Task<Guid> SeedDayAsync(Guid ownerId)
    {
        await using var ctx = CreateContext();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var plan = new Plan
        {
            Name = "Test Plan",
            OwnerId = ownerId,
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
            DayOfWeek = today.DayOfWeek,
            Status = DayStatus.Normal
        };

        ctx.Plans.Add(plan);
        ctx.WeeklyWorkouts.Add(week);
        ctx.DailyWorkouts.Add(day);
        await ctx.SaveChangesAsync();

        return day.Id;
    }
}
