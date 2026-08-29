using Xenoh.Domain.Entities;

namespace Xenoh.Application.Common.Interfaces;

public interface ISubscriptionActivationService
{
    Task ActivateAsync(PaymentOrder order, CancellationToken ct = default);

    Task ActivateComplimentaryAsync(
        PaymentOrder order,
        LegalAcceptance legalAcceptance,
        CancellationToken ct = default);
}
