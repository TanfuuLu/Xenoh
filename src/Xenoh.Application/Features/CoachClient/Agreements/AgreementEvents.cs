using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.CoachClient.Agreements;

public static class AgreementEvents
{
    public static void Record(IApplicationDbContext db, CoachClientRelationship relationship, string kind, Guid? actor,
        DateTime? effectiveAt = null, Guid? agreementId = null)
    {
        var entry = new CoachingAgreementEvent
        {
            RelationshipId = relationship.Id, AgreementId = agreementId ?? relationship.AgreementId,
            ActorId = actor, Kind = kind, OccurredAtUtc = DateTime.UtcNow, EffectiveAtUtc = effectiveAt
        };
        db.CoachingAgreementEvents.Add(entry);
        foreach (var recipient in new[] { relationship.CoachId, relationship.ClientId }.Distinct())
            db.Notifications.Add(new Notification
            {
                RecipientId = recipient, SourceEventId = entry.Id, Type = "Agreement" + kind,
                Message = kind switch
                {
                    "Accepted" => "Coaching agreement accepted. Review your shared agreement.",
                    "Proposed" => "A coaching agreement is ready for your review.",
                    "EndingApprovalRequested" => "The coach requested to end coaching. Client approval is required; coaching continues while the request is pending.",
                    "EndingApprovalDeclined" => "The client declined the ending request. Coaching continues.",
                    "EndingRequested" => "Coaching will end after the agreed notice. View the effective date.",
                    "Ended" => "The coaching agreement has ended. Training history is retained.",
                    "Expired" => "The coaching agreement has expired. Training history is retained.",
                    "ExpiringSoon" => "Your coaching agreement expires soon. Review your next steps.",
                    "Started" => "Your coaching agreement has started.",
                    _ => "Your coaching agreement has been updated. View its activity."
                },
                RelatedEntityId = relationship.Id, RelatedEntityType = "CoachingAgreement"
            });
    }
}
