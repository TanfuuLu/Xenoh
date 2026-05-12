using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Enums;

namespace Xenoh.API.Auth;

public static class SubscriptionPolicies
{
    public const string RequirePro = nameof(RequirePro);
    public const string RequireProCoach = nameof(RequireProCoach);
}

public sealed class ActiveSubscriptionRequirement(params PlanTier[] allowedTiers) : IAuthorizationRequirement
{
    public IReadOnlySet<PlanTier> AllowedTiers { get; } = allowedTiers.ToHashSet();
}

public sealed class ActiveSubscriptionAuthorizationHandler(
    ISubscriptionService subscriptionService
) : AuthorizationHandler<ActiveSubscriptionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveSubscriptionRequirement requirement)
    {
        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
            return;

        var tier = await subscriptionService.GetActiveTierAsync(userId);
        if (requirement.AllowedTiers.Contains(tier))
            context.Succeed(requirement);
    }
}
