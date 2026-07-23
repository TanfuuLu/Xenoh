using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public sealed class SupplementScheduleVersion : BaseEntity
{
    public Guid RegimenId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public SupplementRegimen Regimen { get; set; } = null!;
    public ApplicationUser? CreatedByUser { get; set; }
    public ICollection<SupplementDoseSlot> DoseSlots { get; set; } = [];

    public bool IsEffectiveOn(DateOnly date) =>
        EffectiveFrom <= date && (EffectiveTo is null || EffectiveTo >= date);
}
