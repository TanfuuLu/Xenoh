using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Subscriptions;

public static class SubscriptionLimits
{
    public static int MaxPlans(PlanTier tier) => tier switch
    {
        PlanTier.Free => 3,
        PlanTier.ProIndividual => int.MaxValue,
        PlanTier.ProCoach => int.MaxValue,
        _ => 3
    };

    public static int MaxClients(PlanTier tier) => tier switch
    {
        PlanTier.ProCoach => int.MaxValue,
        _ => 5
    };

    public static bool CanUseAdvancedAnalytics(PlanTier tier) =>
        tier is PlanTier.ProIndividual or PlanTier.ProCoach;

    public static int MaxAiRequestsPerMonth(PlanTier tier) => tier switch
    {
        PlanTier.Free => 0,
        PlanTier.ProIndividual => 100,
        PlanTier.ProCoach => 500,
        _ => 0
    };

    public static int MaxCustomExercises(PlanTier tier) => tier switch
    {
        PlanTier.ProIndividual => int.MaxValue,
        PlanTier.ProCoach => int.MaxValue,
        _ => 0
    };

    public static decimal GetPrice(PlanTier tier, int durationMonths) => (tier, durationMonths) switch
    {
        (PlanTier.ProIndividual, 1) => 149_000m,
        (PlanTier.ProIndividual, 3) => 447_000m,
        (PlanTier.ProIndividual, 6) => 894_000m,
        (PlanTier.ProIndividual, 12) => 1_788_000m,
        (PlanTier.ProCoach, 1) => 199_000m,
        (PlanTier.ProCoach, 3) => 897_000m,
        (PlanTier.ProCoach, 6) => 1_794_000m,
        (PlanTier.ProCoach, 12) => 3_588_000m,
        _ => throw new InvalidOperationException($"No price defined for tier {tier} / {durationMonths} months.")
    };

    public static decimal GetListPrice(PlanTier tier, int durationMonths) => (tier, durationMonths) switch
    {
        (PlanTier.ProIndividual, 1) => 149_000m,
        (PlanTier.ProIndividual, 3) => 447_000m,
        (PlanTier.ProIndividual, 6) => 894_000m,
        (PlanTier.ProIndividual, 12) => 1_788_000m,
        (PlanTier.ProCoach, 1) => 299_000m,
        (PlanTier.ProCoach, 3) => 897_000m,
        (PlanTier.ProCoach, 6) => 1_794_000m,
        (PlanTier.ProCoach, 12) => 3_588_000m,
        _ => throw new InvalidOperationException($"No list price defined for tier {tier} / {durationMonths} months.")
    };
}
