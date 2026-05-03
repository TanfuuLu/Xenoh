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
}
