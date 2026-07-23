using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public sealed class SupplementRegimen : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Form { get; set; }
    public string? Instructions { get; set; }
    public string? Notes { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public ApplicationUser? CreatedByUser { get; set; }
    public ICollection<SupplementScheduleVersion> ScheduleVersions { get; set; } = [];
}
