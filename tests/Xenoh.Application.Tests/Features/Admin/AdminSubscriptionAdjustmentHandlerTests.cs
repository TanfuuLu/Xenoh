using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Xunit;
using Xenoh.Application.Features.Admin;
using Xenoh.Application.Tests.Common;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Tests.Features.Admin;

public sealed class AdminSubscriptionAdjustmentHandlerTests : IdentityHandlerTestBase
{
    private AdjustAdminUserSubscriptionHandler CreateHandler(UserManager<ApplicationUser> userManager)
    {
        var ctx = CreateContext();
        return new AdjustAdminUserSubscriptionHandler(ctx, CurrentUser(), userManager);
    }

    [Fact]
    public async Task Handle_WithPaidTier_CreatesSubscriptionSyncsCoachRoleAndAudits()
    {
        await SeedRolesAsync();
        var targetId = Guid.NewGuid();
        await SeedUserAsync(UserId, "admin@test.com", "password");
        await SeedUserAsync(targetId, "coach@test.com", "password");
        var userManager = CreateUserManager();

        var result = await CreateHandler(userManager).Handle(
            new AdjustAdminUserSubscriptionCommand(targetId, PlanTier.ProCoach, 1, "Manual support grant"),
            CancellationToken.None);

        await using var ctx = CreateContext();
        var subscription = ctx.UserSubscriptions.Single(s => s.UserId == targetId);
        result.Tier.Should().Be(PlanTier.ProCoach);
        subscription.Tier.Should().Be(PlanTier.ProCoach);
        subscription.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddDays(25));
        (await userManager.IsInRoleAsync((await userManager.FindByIdAsync(targetId.ToString()))!, UserRole.Coach)).Should().BeTrue();
        ctx.AdminAuditLogs.Single().Action.Should().Be(AdminAudit.AdjustSubscription);
        ctx.AdminAuditLogs.Single().Reason.Should().Be("Manual support grant");
    }

    [Fact]
    public async Task Handle_WithSameActiveTier_ExtendsFromCurrentExpiry()
    {
        await SeedRolesAsync();
        var targetId = Guid.NewGuid();
        await SeedUserAsync(UserId, "admin@test.com", "password");
        await SeedUserAsync(targetId, "pro@test.com", "password");
        var currentExpiry = DateTime.UtcNow.AddDays(10);
        await using (var seed = CreateContext())
        {
            seed.UserSubscriptions.Add(new UserSubscription
            {
                UserId = targetId,
                Tier = PlanTier.ProIndividual,
                ExpiresAt = currentExpiry
            });
            await seed.SaveChangesAsync();
        }

        await CreateHandler(CreateUserManager()).Handle(
            new AdjustAdminUserSubscriptionCommand(targetId, PlanTier.ProIndividual, 1, "Extend subscription"),
            CancellationToken.None);

        await using var ctx = CreateContext();
        var subscription = ctx.UserSubscriptions.Single(s => s.UserId == targetId);
        subscription.ExpiresAt.Should().BeAfter(currentExpiry.AddDays(25));
        subscription.ExpiresAt.Should().BeBefore(currentExpiry.AddDays(35));
    }

    [Fact]
    public async Task Handle_WithTierChange_StartsNewTermFromNow()
    {
        await SeedRolesAsync();
        var targetId = Guid.NewGuid();
        await SeedUserAsync(UserId, "admin@test.com", "password");
        await SeedUserAsync(targetId, "switch@test.com", "password");
        await using (var seed = CreateContext())
        {
            seed.UserSubscriptions.Add(new UserSubscription
            {
                UserId = targetId,
                Tier = PlanTier.ProIndividual,
                ExpiresAt = DateTime.UtcNow.AddDays(100)
            });
            await seed.SaveChangesAsync();
        }

        await CreateHandler(CreateUserManager()).Handle(
            new AdjustAdminUserSubscriptionCommand(targetId, PlanTier.ProCoach, 1, "Upgrade for coach trial"),
            CancellationToken.None);

        await using var ctx = CreateContext();
        var subscription = ctx.UserSubscriptions.Single(s => s.UserId == targetId);
        subscription.Tier.Should().Be(PlanTier.ProCoach);
        subscription.ExpiresAt.Should().BeBefore(DateTime.UtcNow.AddDays(40));
    }

    [Fact]
    public async Task Handle_WithFreeTier_CancelsSubscriptionAndRemovesCoachRole()
    {
        await SeedRolesAsync();
        var targetId = Guid.NewGuid();
        await SeedUserAsync(UserId, "admin@test.com", "password");
        await SeedUserAsync(targetId, "cancel@test.com", "password");
        var userManager = CreateUserManager();
        var target = (await userManager.FindByIdAsync(targetId.ToString()))!;
        await userManager.AddToRoleAsync(target, UserRole.Coach);
        await using (var seed = CreateContext())
        {
            seed.UserSubscriptions.Add(new UserSubscription
            {
                UserId = targetId,
                Tier = PlanTier.ProCoach,
                ExpiresAt = DateTime.UtcNow.AddDays(20)
            });
            await seed.SaveChangesAsync();
        }

        await CreateHandler(userManager).Handle(
            new AdjustAdminUserSubscriptionCommand(targetId, PlanTier.Free, null, "Cancel manual grant"),
            CancellationToken.None);

        await using var ctx = CreateContext();
        var subscription = ctx.UserSubscriptions.Single(s => s.UserId == targetId);
        subscription.Tier.Should().Be(PlanTier.Free);
        subscription.ExpiresAt.Should().BeNull();
        (await userManager.IsInRoleAsync(target, UserRole.Coach)).Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WithMissingReason_Throws(string reason)
    {
        await SeedRolesAsync();
        var targetId = Guid.NewGuid();
        await SeedUserAsync(targetId, "invalid@test.com", "password");

        var act = () => CreateHandler(CreateUserManager()).Handle(
            new AdjustAdminUserSubscriptionCommand(targetId, PlanTier.ProIndividual, 1, reason),
            CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Adjustment reason is required.");
    }

    [Fact]
    public async Task Handle_WithInvalidPaidDuration_Throws()
    {
        await SeedRolesAsync();
        var targetId = Guid.NewGuid();
        await SeedUserAsync(targetId, "invalid-duration@test.com", "password");

        var act = () => CreateHandler(CreateUserManager()).Handle(
            new AdjustAdminUserSubscriptionCommand(targetId, PlanTier.ProIndividual, 2, "Invalid duration"),
            CancellationToken.None).AsTask();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Paid subscription adjustments require a duration of 1, 3, 6, or 12 months.");
    }
}
