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
    ISupplementRepository supplementRepo,
    IApplicationDbContext db,
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

        if (relationship.Status == RelationshipStatus.Ended)
            throw new InvalidOperationException("This relationship has already ended.");

        var wasPending = relationship.Status == RelationshipStatus.Pending;

        var otherPartyId = userId == relationship.ClientId
            ? relationship.CoachId
            : relationship.ClientId;

        // A request that was never accepted has no coaching work behind it, so there is
        // nothing to tear down — only established relationships own plans and shares.
        if (!wasPending)
        {
            await CoachResourceCleanup.StageAsync(
                db,
                planRepo,
                supplementRepo,
                relationship.ClientId,
                relationship.CoachId,
                cancellationToken);
        }

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
            wasPending ? "ConnectionRequestCancelled" : "DisconnectCompleted",
            wasPending
                ? $"{initiatorName} đã hủy yêu cầu kết nối."
                : $"{initiatorName} đã ngắt kết nối với bạn.",
            relationship.Id,
            "CoachRequest",
            cancellationToken);

        return Unit.Value;
    }
}
