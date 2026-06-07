using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Xenoh.API.Auth;
using Xenoh.API.Controllers;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Enums;
using Xunit;

namespace Xenoh.Application.Tests.Features.Subscriptions;

public sealed class SubscriptionAuthorizationTests
{
    [Fact]
    public async Task RequirePro_AllowsActiveProIndividual()
    {
        var userId = Guid.NewGuid();
        var requirement = new ActiveSubscriptionRequirement(PlanTier.ProIndividual, PlanTier.ProCoach);
        var context = CreateContext(userId, requirement);
        var handler = new ActiveSubscriptionAuthorizationHandler(new StubSubscriptionService(PlanTier.ProIndividual));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task RequireProCoach_DeniesProIndividual()
    {
        var userId = Guid.NewGuid();
        var requirement = new ActiveSubscriptionRequirement(PlanTier.ProCoach);
        var context = CreateContext(userId, requirement);
        var handler = new ActiveSubscriptionAuthorizationHandler(new StubSubscriptionService(PlanTier.ProIndividual));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task RequirePro_DeniesExpiredSubscriptionReturnedAsFree()
    {
        var userId = Guid.NewGuid();
        var requirement = new ActiveSubscriptionRequirement(PlanTier.ProIndividual, PlanTier.ProCoach);
        var context = CreateContext(userId, requirement);
        var handler = new ActiveSubscriptionAuthorizationHandler(new StubSubscriptionService(PlanTier.Free));

        await handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Theory]
    [InlineData(typeof(InsightsController), nameof(InsightsController.GetMyAnalysis), SubscriptionPolicies.RequirePro)]
    [InlineData(typeof(PlansController), nameof(PlansController.CreateAiStarterPlan), SubscriptionPolicies.RequirePro)]
    [InlineData(typeof(PlansController), nameof(PlansController.GetPlanAnalytics), SubscriptionPolicies.RequirePro)]
    [InlineData(typeof(PlansController), nameof(PlansController.ReviewPlanBalance), SubscriptionPolicies.RequirePro)]
    [InlineData(typeof(PlansController), nameof(PlansController.GetCoachPlans), SubscriptionPolicies.RequireProCoach)]
    [InlineData(typeof(PlansController), nameof(PlansController.CreatePlanForUser), SubscriptionPolicies.RequireProCoach)]
    [InlineData(typeof(CoachClientController), nameof(CoachClientController.GetPendingRequests), SubscriptionPolicies.RequireProCoach)]
    [InlineData(typeof(CoachClientController), nameof(CoachClientController.GetMyClients), SubscriptionPolicies.RequireProCoach)]
    [InlineData(typeof(CoachClientController), nameof(CoachClientController.GetDashboard), SubscriptionPolicies.RequireProCoach)]
    [InlineData(typeof(CoachClientController), nameof(CoachClientController.GetClientPowerlifting), SubscriptionPolicies.RequireProCoach)]
    [InlineData(typeof(CoachClientController), nameof(CoachClientController.GetClientAiBrief), SubscriptionPolicies.RequireProCoach)]
    public void ProEndpoints_HaveExpectedSubscriptionPolicy(Type controllerType, string actionName, string expectedPolicy)
    {
        var method = controllerType.GetMethods()
            .Single(m => m.Name == actionName);

        var policies = method.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .Select(a => a.Policy);

        policies.Should().Contain(expectedPolicy);
    }

    private static AuthorizationHandlerContext CreateContext(Guid userId, IAuthorizationRequirement requirement)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            authenticationType: "Test");
        return new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(identity), resource: null);
    }

    private sealed class StubSubscriptionService(PlanTier tier) : ISubscriptionService
    {
        public Task<PlanTier> GetActiveTierAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(tier);

        public Task<int> GetMaxPlansAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(SubscriptionLimits(tier));

        public Task<int> GetMaxClientsAsync(Guid coachId, CancellationToken ct = default) =>
            Task.FromResult(SubscriptionLimits(tier));

        public Task<bool> CanUseAdvancedAnalyticsAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(tier is PlanTier.ProIndividual or PlanTier.ProCoach);

        private static int SubscriptionLimits(PlanTier currentTier) =>
            currentTier == PlanTier.Free ? 0 : int.MaxValue;
    }
}
