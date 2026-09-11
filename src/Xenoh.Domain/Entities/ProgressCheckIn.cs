using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class ProgressCheckIn : BaseEntity
{
    public Guid OwnerId { get; set; }
    public ApplicationUser Owner { get; set; } = null!;
    public DateOnly CheckInDate { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
    public decimal? BodyweightKg { get; set; }
    public bool IsMilestone { get; set; }
    public bool IsSharedWithCoach { get; set; }
    public ICollection<ProgressPhoto> Photos { get; set; } = [];
}
