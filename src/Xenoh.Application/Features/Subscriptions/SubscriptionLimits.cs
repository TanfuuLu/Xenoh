using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Subscriptions;

public static class SubscriptionLimits
{
    private const long Megabyte = 1024L * 1024;
    private const long Gigabyte = 1024L * Megabyte;

    public static int MaxPlans(PlanTier tier) => tier switch
    {
        PlanTier.Free => 3,
        PlanTier.ProIndividual => int.MaxValue,
        PlanTier.ProCoach => int.MaxValue,
        PlanTier.Organizer => int.MaxValue,
        _ => 3
    };

    public static int MaxClients(PlanTier tier) => tier switch
    {
        PlanTier.ProCoach or PlanTier.Organizer => int.MaxValue,
        _ => 5
    };

    public static bool CanUseAdvancedAnalytics(PlanTier tier) =>
        tier is PlanTier.ProIndividual or PlanTier.ProCoach or PlanTier.Organizer;

    public static int MaxAiRequestsPerMonth(PlanTier tier) => tier switch
    {
        PlanTier.Free => 0,
        PlanTier.ProIndividual => 100,
        PlanTier.ProCoach => 500,
        PlanTier.Organizer => 500,
        _ => 0
    };

    public static int MaxCustomExercises(PlanTier tier) => tier switch
    {
        PlanTier.Free => 10,
        PlanTier.ProIndividual => int.MaxValue,
        PlanTier.ProCoach => int.MaxValue,
        PlanTier.Organizer => int.MaxValue,
        _ => 10
    };

    /// <summary>Largest single document a user may upload (0 = feature unavailable).</summary>
    public static long MaxFileSizeBytes(PlanTier tier) => tier switch
    {
        PlanTier.Free or PlanTier.ProIndividual or PlanTier.ProCoach or PlanTier.Organizer => 25L * Megabyte,
        _ => 0
    };

    /// <summary>Total bytes a user may keep stored across all their documents.</summary>
    public static long MaxStorageBytes(PlanTier tier) => tier switch
    {
        PlanTier.Free => 250L * Megabyte,
        PlanTier.ProIndividual => 1L * Gigabyte,
        PlanTier.ProCoach => 5L * Gigabyte,
        PlanTier.Organizer => 5L * Gigabyte,
        _ => 0
    };

    public static decimal GetPrice(PlanTier tier, int durationMonths) =>
        SubscriptionCatalog.GetRequired(tier, durationMonths).Price;

    public static decimal GetListPrice(PlanTier tier, int durationMonths) =>
        SubscriptionCatalog.GetRequired(tier, durationMonths).Price;
}
