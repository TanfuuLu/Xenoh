using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Commands.EndRelationship;

/// <summary>
/// Ends a coach-client relationship immediately at the request of either participant.
/// Disconnecting is one-sided by design: the other party is notified, not asked.
/// </summary>
public sealed class EndRelationshipHandler(
    ICoachClientRepository coachClientRepo,
    IPlanRepository planRepo,
    ICurrentUserService currentUser,
    INotificationService notificationService,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<EndRelationshipCommand>
{
    public async ValueTask<Unit> Handle(EndRelationshipCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var relationship = await coachClientRepo.FindByIdForParticipantAsync(
            request.RelationshipId, userId, cancellationToken)
            ?? throw new InvalidOperationException("Relationship not found.");

        if (relationship.Status != RelationshipStatus.Active &&
            relationship.Status != RelationshipStatus.Expired &&
            relationship.Status != RelationshipStatus.PendingRenewal &&
            relationship.Status != RelationshipStatus.PendingTermination)
            throw new InvalidOperationException("Only an established relationship can be ended.");

        var otherPartyId = userId == relationship.ClientId
            ? relationship.CoachId
            : relationship.ClientId;

        await planRepo.DeleteCoachPlansForClientAsync(
            relationship.ClientId, relationship.CoachId, cancellationToken);

        relationship.Status = RelationshipStatus.Ended;
        relationship.TerminationRequestedBy = null;
        relationship.RenewalRequestedBy = null;
        relationship.ProposedEndDate = null;
        relationship.UpdatedAt = DateTime.UtcNow;

        await coachClientRepo.SaveChangesAsync(cancellationToken);

        var initiator = await userManager.FindByIdAsync(userId.ToString());
        var initiatorName = initiator is null ? "Someone" : $"{initiator.FirstName} {initiator.LastName}";

        await notificationService.NotifyAsync(
            otherPartyId,
            "DisconnectCompleted",
            $"{initiatorName} đã ngắt kết nối với bạn.",
            relationship.Id,
            "CoachRequest",
            cancellationToken);

        return Unit.Value;
    }
}
