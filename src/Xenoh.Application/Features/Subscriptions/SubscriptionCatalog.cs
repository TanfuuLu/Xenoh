using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Subscriptions;

public sealed record SubscriptionOffer(
    PlanTier Tier,
    int DurationMonths,
    decimal Price,
    string Currency,
    bool IsPrepaid,
    bool AutomaticallyRenews,
    bool HasUnlimitedClients);

public static class SubscriptionCatalog
{
    public static readonly IReadOnlyList<SubscriptionOffer> PublicPlans =
    [
        Offer(PlanTier.ProIndividual, 1, 100_000m),
        Offer(PlanTier.ProIndividual, 3, 300_000m),
        Offer(PlanTier.ProIndividual, 6, 600_000m),
        Offer(PlanTier.ProIndividual, 12, 1_200_000m),
        Offer(PlanTier.ProCoach, 1, 199_000m),
        Offer(PlanTier.ProCoach, 3, 597_000m),
        Offer(PlanTier.ProCoach, 6, 1_194_000m),
        Offer(PlanTier.ProCoach, 12, 2_388_000m)
    ];

    public static SubscriptionOffer GetRequired(PlanTier tier, int durationMonths) =>
        PublicPlans.SingleOrDefault(x => x.Tier == tier && x.DurationMonths == durationMonths)
        ?? throw new InvalidOperationException("Invalid tier/duration combination.");

    private static SubscriptionOffer Offer(PlanTier tier, int months, decimal price) =>
        new(
            tier,
            months,
            price,
            "VND",
            IsPrepaid: true,
            AutomaticallyRenews: false,
            HasUnlimitedClients: tier == PlanTier.ProCoach);
}
