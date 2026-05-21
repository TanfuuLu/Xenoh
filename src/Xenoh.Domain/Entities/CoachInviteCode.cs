using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public class CoachInviteCode : BaseEntity
{
    public Guid CoachId { get; set; }
    public ApplicationUser Coach { get; set; } = null!;

    /// <summary>8-character alphanumeric, unique.</summary>
    public string Code { get; set; } = string.Empty;

    public DateOnly CoachingStartDate { get; set; }
    public DateOnly CoachingEndDate { get; set; }

    public bool IsUsed { get; set; }
    public Guid? UsedByClientId { get; set; }
    public DateTime? UsedAt { get; set; }
}
