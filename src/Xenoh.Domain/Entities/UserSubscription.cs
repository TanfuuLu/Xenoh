using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class UserSubscription : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public PlanTier Tier { get; set; } = PlanTier.Free;

    public DateTime? ExpiresAt { get; set; }

    // Free tier never expires; paid tiers check expiry lazily
    public bool IsActive => Tier == PlanTier.Free || (ExpiresAt.HasValue && ExpiresAt.Value > DateTime.UtcNow);

    public ICollection<PaymentOrder> PaymentOrders { get; set; } = [];
}
