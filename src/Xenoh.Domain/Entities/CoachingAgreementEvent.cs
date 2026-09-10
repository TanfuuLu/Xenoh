using Xenoh.Domain.Common;

namespace Xenoh.Domain.Entities;

public sealed class CoachingAgreementEvent : BaseEntity
{
    public Guid RelationshipId { get; set; }
    public Guid? AgreementId { get; set; }
    public Guid? ActorId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public DateTime? EffectiveAtUtc { get; set; }
}
