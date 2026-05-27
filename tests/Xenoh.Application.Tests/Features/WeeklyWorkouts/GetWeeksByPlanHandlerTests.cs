using FluentAssertions;
using Xunit;
using Xenoh.Application.Features.WeeklyWorkouts.Queries.GetWeeksByPlan;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;

namespace Xenoh.Application.Tests.Features.WeeklyWorkouts;

public sealed class GetWeeksByPlanHandlerTests : HandlerTestBase
{
    private GetWeeksByPlanHandler CreateHandler(ApplicationDbContext ctx) =>
        new(new WeeklyWorkoutRepository(ctx), CurrentUser());

    [Fact]
    public async Task Handle_WhenPlanAccessible_ReturnsOrderedWeeks()
    {
        var planId = await SeedPlanWithWeeksAsync(UserId, weekCount: 3);

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(new GetWeeksByPlanQuery(planId), CancellationToken.None);

        result.Should().HaveCount(3);
        result.Select(w => w.WeekNumber).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task Handle_WhenPlanNotAccessible_Throws()
    {
        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new GetWeeksByPlanQuery(Guid.NewGuid()), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Plan not found.");
    }

    [Fact]
    public async Task Handle_WhenCallerIsCoach_ReturnsWeeks()
    {
        var planId = await SeedCoachPlanWithWeeksAsync(clientId: Guid.NewGuid(), coachId: UserId);

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(new GetWeeksByPlanQuery(planId), CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WhenOtherUsersPlan_Throws()
    {
        var planId = await SeedPlanWithWeeksAsync(ownerId: Guid.NewGuid(), weekCount: 1);

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new GetWeeksByPlanQuery(planId), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Plan not found.");
    }

    private async Task<Guid> SeedPlanWithWeeksAsync(Guid ownerId, int weekCount)
    {
        await using var ctx = CreateContext();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var plan = new Plan
        {
            Name = "Test Plan",
            OwnerId = ownerId,
            PlanType = PlanType.Self,
            StartDate = today,
            EndDate = today.AddDays(weekCount * 7 - 1)
        };
        ctx.Plans.Add(plan);
        for (var i = 0; i < weekCount; i++)
        {
            ctx.WeeklyWorkouts.Add(new WeeklyWorkout
            {
                PlanId = plan.Id,
                WeekNumber = i + 1,
                Name = $"Week {i + 1}",
                StartDate = today.AddDays(i * 7),
                EndDate = today.AddDays(i * 7 + 6)
            });
        }
        await ctx.SaveChangesAsync();
        return plan.Id;
    }

    private async Task<Guid> SeedCoachPlanWithWeeksAsync(Guid clientId, Guid coachId)
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
            EndDate = today.AddDays(6)
        };
        ctx.Plans.Add(plan);
        ctx.WeeklyWorkouts.Add(new WeeklyWorkout
        {
            PlanId = plan.Id,
            WeekNumber = 1,
            Name = "Week 1",
            StartDate = today,
            EndDate = today.AddDays(6)
        });
        await ctx.SaveChangesAsync();
        return plan.Id;
    }
}
