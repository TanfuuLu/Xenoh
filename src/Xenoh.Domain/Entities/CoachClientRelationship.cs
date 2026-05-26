using Xenoh.Domain.Common;
using Xenoh.Domain.Enums;

namespace Xenoh.Domain.Entities;

public class CoachClientRelationship : BaseEntity
{
    public Guid ClientId { get; set; }
    public ApplicationUser Client { get; set; } = null!;

    public Guid CoachId { get; set; }
    public ApplicationUser Coach { get; set; } = null!;

    public RelationshipStatus Status { get; set; } = RelationshipStatus.Pending;

    public Guid? TerminationRequestedBy { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public Guid? RenewalRequestedBy { get; set; }
    public DateOnly? ProposedEndDate { get; set; }

    /// <summary>Set when the relationship was created via a Coach Invite Code.</summary>
    public Guid? CoachInviteCodeId { get; set; }
    public CoachInviteCode? CoachInviteCode { get; set; }
}
