using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class AiUsageQuota : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public DateOnly PeriodStart { get; set; }
    public int UsedRequests { get; set; }
    public string? LastFeature { get; set; }
    public DateTime? LastConsumedAt { get; set; }
}
