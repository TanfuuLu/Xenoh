using FluentAssertions;
using Xunit;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;
using Xenoh.Infrastructure.Persistence;
using Xenoh.Infrastructure.Persistence.Repositories;
using Xenoh.Infrastructure.Services;

namespace Xenoh.Application.Tests.Features.Plans;

public sealed class CreatePlanHandlerTests : HandlerTestBase
{
    private CreatePlanHandler CreateHandler(ApplicationDbContext ctx) =>
        new(new PlanRepository(ctx), CurrentUser(), new SubscriptionService(new SubscriptionRepository(ctx)));

    [Fact]
    public async Task Handle_WhenValidRequest_CreatesPlanWithWeeks()
    {
        await SeedUserAsync(UserId);
        var start = DateOnly.FromDateTime(DateTime.Today);

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(new CreatePlanCommand
        {
            Name = "Strength Plan",
            StartDate = start,
            EndDate = start.AddDays(27)
        }, CancellationToken.None);

        result.Id.Should().NotBe(Guid.Empty);
        result.Name.Should().Be("Strength Plan");
        result.TotalWeeks.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_WhenFirstPlan_AutoActivates()
    {
        await SeedUserAsync(UserId);
        var start = DateOnly.FromDateTime(DateTime.Today);

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(new CreatePlanCommand
        {
            Name = "My First Plan",
            StartDate = start,
            EndDate = start.AddDays(27)
        }, CancellationToken.None);

        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenSecondPlan_DoesNotAutoActivate()
    {
        await SeedUserAsync(UserId);
        await SeedExistingPlanAsync(UserId);
        var start = DateOnly.FromDateTime(DateTime.Today);

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(new CreatePlanCommand
        {
            Name = "Second Plan",
            StartDate = start,
            EndDate = start.AddDays(27)
        }, CancellationToken.None);

        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenEndDateBeforeStartDate_Throws()
    {
        var start = DateOnly.FromDateTime(DateTime.Today);

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new CreatePlanCommand
        {
            Name = "Bad Plan",
            StartDate = start,
            EndDate = start.AddDays(-1)
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("End date must be after start date.");
    }

    [Fact]
    public async Task Handle_WhenFreeUserAtPlanLimit_Throws()
    {
        await SeedUserAsync(UserId);
        for (var i = 0; i < 3; i++)
            await SeedExistingPlanAsync(UserId);

        var start = DateOnly.FromDateTime(DateTime.Today);

        await using var ctx = CreateContext();
        var act = () => CreateHandler(ctx).Handle(new CreatePlanCommand
        {
            Name = "Plan 4",
            StartDate = start,
            EndDate = start.AddDays(27)
        }, CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*maximum of 3 plans*");
    }

    [Fact]
    public async Task Handle_WhenProUserExceedsFreeLimit_CreatesSuccessfully()
    {
        await SeedUserAsync(UserId);
        await SeedActiveSubscriptionAsync(UserId, PlanTier.ProIndividual);
        for (var i = 0; i < 5; i++)
            await SeedExistingPlanAsync(UserId);

        var start = DateOnly.FromDateTime(DateTime.Today);

        await using var ctx = CreateContext();
        var result = await CreateHandler(ctx).Handle(new CreatePlanCommand
        {
            Name = "Plan 6",
            StartDate = start,
            EndDate = start.AddDays(27)
        }, CancellationToken.None);

        result.Id.Should().NotBe(Guid.Empty);
    }

    private async Task SeedUserAsync(Guid userId)
    {
        await using var ctx = CreateContext();
        ctx.Users.Add(new ApplicationUser
        {
            Id = userId,
            FirstName = "Test",
            LastName = "User",
            Email = "test@test.com",
            UserName = "test@test.com"
        });
        await ctx.SaveChangesAsync();
    }

    private async Task SeedExistingPlanAsync(Guid ownerId)
    {
        await using var ctx = CreateContext();
        ctx.Plans.Add(new Plan
        {
            Name = "Seed Plan",
            OwnerId = ownerId,
            PlanType = PlanType.Self,
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(27))
        });
        await ctx.SaveChangesAsync();
    }

    private async Task SeedActiveSubscriptionAsync(Guid userId, PlanTier tier)
    {
        await using var ctx = CreateContext();
        ctx.UserSubscriptions.Add(new UserSubscription
        {
            UserId = userId,
            Tier = tier,
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        });
        await ctx.SaveChangesAsync();
    }
}
