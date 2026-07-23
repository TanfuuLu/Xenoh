using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public sealed class SupplementIntakeLog : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid DoseSlotId { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public SupplementIntakeStatus Status { get; set; }
    public DateTime RecordedAt { get; set; }
    public string? Note { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public SupplementDoseSlot DoseSlot { get; set; } = null!;
}
