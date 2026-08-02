using Mediator;

namespace Xenoh.Application.Features.Subscriptions.Queries.GetSubscriptionCatalog;

public sealed record GetSubscriptionCatalogQuery : IRequest<SubscriptionCatalogResponse>;

public sealed record SubscriptionCatalogOfferResponse(
    string Tier,
    int DurationMonths,
    decimal Price,
    string Currency,
    bool IsPrepaid,
    bool AutomaticallyRenews,
    bool HasUnlimitedClients);

public sealed record SubscriptionCatalogResponse(
    string TermsVersion,
    IReadOnlyList<SubscriptionCatalogOfferResponse> Offers);
