using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Commands.RejectTermination;

public sealed class RejectTerminationHandler(
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser,
    INotificationService notificationService,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<RejectTerminationCommand>
{
    public async ValueTask<Unit> Handle(RejectTerminationCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var relationship = await coachClientRepo.FindByIdForParticipantAsync(
            request.RelationshipId, userId, cancellationToken)
            ?? throw new InvalidOperationException("Relationship not found.");

        if (relationship.Status != RelationshipStatus.PendingTermination)
            throw new InvalidOperationException("No pending termination request for this relationship.");

        var initiatorId = relationship.TerminationRequestedBy!.Value;
        var otherPartyId = userId == relationship.ClientId
            ? relationship.CoachId
            : relationship.ClientId;

        relationship.Status = RelationshipStatus.Active;
        relationship.TerminationRequestedBy = null;
        relationship.UpdatedAt = DateTime.UtcNow;

        await coachClientRepo.SaveChangesAsync(cancellationToken);

        var actor = await userManager.FindByIdAsync(userId.ToString());
        var actorName = actor is null ? "Someone" : $"{actor.FirstName} {actor.LastName}";

        if (userId != initiatorId)
        {
            // Other party rejected the request — notify the initiator
            await notificationService.NotifyAsync(
                initiatorId,
                "DisconnectRejected",
                "Yêu cầu ngắt kết nối của bạn đã bị từ chối.",
                relationship.Id,
                "CoachRequest",
                cancellationToken);
        }
        else
        {
            // Initiator cancelled their own request — notify the other party
            await notificationService.NotifyAsync(
                otherPartyId,
                "DisconnectCancelled",
                $"{actorName} đã hủy yêu cầu ngắt kết nối.",
                relationship.Id,
                "CoachRequest",
                cancellationToken);
        }

        return Unit.Value;
    }
}
