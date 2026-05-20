using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Subscriptions;

public static class SubscriptionLimits
{
    public static int MaxPlans(PlanTier tier) => tier switch
    {
        PlanTier.Free          => 3,
        PlanTier.ProIndividual => int.MaxValue,
        PlanTier.ProCoach      => int.MaxValue,
        _                      => 3
    };

    public static int MaxClients(PlanTier tier) => tier switch
    {
        PlanTier.ProCoach => int.MaxValue,
        _                 => 5
    };

    public static bool CanUseAdvancedAnalytics(PlanTier tier) =>
        tier is PlanTier.ProIndividual or PlanTier.ProCoach;

    public static int MaxCustomExercises(PlanTier tier) => tier switch
    {
        PlanTier.ProIndividual => int.MaxValue,
        PlanTier.ProCoach      => int.MaxValue,
        _                      => 0
    };

    public static decimal GetPrice(PlanTier tier, int durationMonths) => (tier, durationMonths) switch
    {
        (PlanTier.ProIndividual, 1)  => 99_000m,
        (PlanTier.ProIndividual, 3)  => 297_000m,
        (PlanTier.ProIndividual, 12) => 1_188_000m,
        (PlanTier.ProCoach,      1)  => 199_000m,
        (PlanTier.ProCoach,      3)  => 597_000m,
        (PlanTier.ProCoach,      12) => 2_388_000m,
        _ => throw new InvalidOperationException($"No price defined for tier {tier} / {durationMonths} months.")
    };
}
