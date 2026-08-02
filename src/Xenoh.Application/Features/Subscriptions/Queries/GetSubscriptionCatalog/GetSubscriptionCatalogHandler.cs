using Mediator;

namespace Xenoh.Application.Features.Subscriptions.Queries.GetSubscriptionCatalog;

public sealed class GetSubscriptionCatalogHandler
    : IRequestHandler<GetSubscriptionCatalogQuery, SubscriptionCatalogResponse>
{
    public ValueTask<SubscriptionCatalogResponse> Handle(
        GetSubscriptionCatalogQuery request,
        CancellationToken cancellationToken)
    {
        var offers = SubscriptionCatalog.PublicPlans
            .Select(x => new SubscriptionCatalogOfferResponse(
                x.Tier.ToString(),
                x.DurationMonths,
                x.Price,
                x.Currency,
                x.IsPrepaid,
                x.AutomaticallyRenews,
                x.HasUnlimitedClients))
            .ToArray();

        return ValueTask.FromResult(new SubscriptionCatalogResponse(
            SubscriptionContract.CurrentTermsVersion,
            offers));
    }
}
