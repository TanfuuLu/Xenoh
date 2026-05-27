using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xenoh.Application.Features.Plans.Commands.DeletePlan;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;

namespace Xenoh.Application.Tests.Features.Plans;

public sealed class DeletePlanHandlerTests : HandlerTestBase
{
    private DeletePlanHandler CreateHandler(ApplicationDbContext ctx) =>
        new(new PlanRepository(ctx), CurrentUser());

    [Fact]
    public async Task Handle_WhenOwnerDeletesOwnPlan_RemovesPlan()
    {
        var planId = await SeedPlanAsync(UserId, PlanType.Self);

        await using var ctx = CreateContext();
        await CreateHandler(ctx).Handle(new DeletePlanCommand { PlanId = planId }, CancellationToken.None);

        await using var verify = CreateContext();
        var exists = await verify.Plans.AnyAsync(p => p.Id == planId);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenNonOwnerDeletes_Throws()
    {
        var otherUserId = Guid.NewGuid();
        var planId = await SeedPlanAsync(otherUserId, PlanType.Self);

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new DeletePlanCommand { PlanId = planId }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Plan not found.");
    }

    [Fact]
    public async Task Handle_WhenCoachDeletesCoachPlan_RemovesPlan()
    {
        var clientId = Guid.NewGuid();
        var planId = await SeedCoachPlanAsync(clientId, UserId);

        await using var ctx = CreateContext();
        await CreateHandler(ctx).Handle(new DeletePlanCommand { PlanId = planId }, CancellationToken.None);

        await using var verify = CreateContext();
        var exists = await verify.Plans.AnyAsync(p => p.Id == planId);
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenPlanNotFound_Throws()
    {
        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new DeletePlanCommand { PlanId = Guid.NewGuid() }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Plan not found.");
    }

    private async Task<Guid> SeedPlanAsync(Guid ownerId, PlanType planType)
    {
        await using var ctx = CreateContext();
        var plan = new Plan
        {
            Name = "Test Plan",
            OwnerId = ownerId,
            PlanType = planType,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(27))
        };
        ctx.Plans.Add(plan);
        await ctx.SaveChangesAsync();
        return plan.Id;
    }

    private async Task<Guid> SeedCoachPlanAsync(Guid clientId, Guid coachId)
    {
        await using var ctx = CreateContext();
        var plan = new Plan
        {
            Name = "Coach Plan",
            OwnerId = clientId,
            CreatedByCoachId = coachId,
            PlanType = PlanType.Coach,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(27))
        };
        ctx.Plans.Add(plan);
        await ctx.SaveChangesAsync();
        return plan.Id;
    }
}
