using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xenoh.Application.Features.Plans.Commands.ActivatePlan;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;

namespace Xenoh.Application.Tests.Features.Plans;

public sealed class ActivatePlanHandlerTests : HandlerTestBase
{
    private ActivatePlanHandler CreateHandler(ApplicationDbContext ctx) =>
        new(new PlanRepository(ctx), CurrentUser());

    [Fact(Skip = "EF InMemory does not support ExecuteUpdateAsync — DeactivateOthersAsync is always called for inactive plans")]
    public async Task Handle_WhenActivatingInactivePlan_SetsActive()
    {
        var planId = await SeedPlanAsync(UserId, isActive: false);

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(new ActivatePlanCommand(planId), CancellationToken.None);

        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenActivatingAlreadyActivePlan_IsIdempotent()
    {
        var planId = await SeedPlanAsync(UserId, isActive: true);

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(new ActivatePlanCommand(planId), CancellationToken.None);

        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenNonOwnerActivates_Throws()
    {
        var otherUserId = Guid.NewGuid();
        var planId = await SeedPlanAsync(otherUserId, isActive: false);

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new ActivatePlanCommand(planId), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Access denied.");
    }

    [Fact]
    public async Task Handle_WhenPlanNotFound_Throws()
    {
        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new ActivatePlanCommand(Guid.NewGuid()), CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Plan not found.");
    }

    [Fact(Skip = "EF InMemory does not support ExecuteUpdateAsync with matching rows — DeactivateOthersAsync requires a real database")]
    public async Task Handle_WhenActivating_DeactivatesOtherPlans()
    {
        var (otherPlanId1, otherPlanId2, targetPlanId) = await SeedThreePlansAsync();

        await using var ctx = CreateContext();
        await CreateHandler(ctx).Handle(new ActivatePlanCommand(targetPlanId), CancellationToken.None);

        await using var verify = CreateContext();
        var plans = await verify.Plans.Where(p => p.OwnerId == UserId).ToListAsync();
        plans.Should().ContainSingle(p => p.IsActive)
             .Which.Id.Should().Be(targetPlanId);
        plans.First(p => p.Id == otherPlanId1).IsActive.Should().BeFalse();
        plans.First(p => p.Id == otherPlanId2).IsActive.Should().BeFalse();
    }

    private async Task<Guid> SeedPlanAsync(Guid ownerId, bool isActive)
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
        var plan = new Plan
        {
            Name = "Test Plan",
            OwnerId = ownerId,
            PlanType = PlanType.Self,
            IsActive = isActive,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(27))
        };
        ctx.Plans.Add(plan);
        await ctx.SaveChangesAsync();
        return plan.Id;
    }

    private async Task<(Guid OtherPlanId1, Guid OtherPlanId2, Guid TargetPlanId)> SeedThreePlansAsync()
    {
        await using var ctx = CreateContext();
        ctx.Users.Add(new ApplicationUser
        {
            Id = UserId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@test.com",
            UserName = "test@test.com"
        });

        var plan1 = new Plan { Name = "Plan 1", OwnerId = UserId, PlanType = PlanType.Self, IsActive = true, StartDate = DateOnly.FromDateTime(DateTime.Today), EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(27)) };
        var plan2 = new Plan { Name = "Plan 2", OwnerId = UserId, PlanType = PlanType.Self, IsActive = true, StartDate = DateOnly.FromDateTime(DateTime.Today), EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(27)) };
        var plan3 = new Plan { Name = "Plan 3", OwnerId = UserId, PlanType = PlanType.Self, IsActive = false, StartDate = DateOnly.FromDateTime(DateTime.Today), EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(27)) };

        ctx.Plans.AddRange(plan1, plan2, plan3);
        await ctx.SaveChangesAsync();
        return (plan1.Id, plan2.Id, plan3.Id);
    }
}
