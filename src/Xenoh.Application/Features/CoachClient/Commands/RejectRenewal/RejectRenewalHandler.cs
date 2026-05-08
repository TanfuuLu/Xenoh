using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Commands.RejectRenewal;

public sealed class RejectRenewalHandler(
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser,
    INotificationService notificationService,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<RejectRenewalCommand>
{
    public async ValueTask<Unit> Handle(RejectRenewalCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var relationship = await coachClientRepo.FindByIdForParticipantAsync(
            request.RelationshipId, userId, cancellationToken)
            ?? throw new InvalidOperationException("Relationship not found.");

        if (relationship.Status != RelationshipStatus.PendingRenewal)
            throw new InvalidOperationException("No pending renewal for this relationship.");

        var initiatorId = relationship.RenewalRequestedBy!.Value;
        var otherPartyId = userId == relationship.ClientId ? relationship.CoachId : relationship.ClientId;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        relationship.Status = relationship.EndDate is { } end && end < today
            ? RelationshipStatus.Expired
            : RelationshipStatus.Active;
        relationship.RenewalRequestedBy = null;
        relationship.ProposedEndDate = null;
        relationship.UpdatedAt = DateTime.UtcNow;

        await coachClientRepo.SaveChangesAsync(cancellationToken);

        var actor = await userManager.FindByIdAsync(userId.ToString());
        var actorName = actor is null ? "Đối tác" : $"{actor.FirstName} {actor.LastName}";

        if (userId != initiatorId)
        {
            await notificationService.NotifyAsync(
                initiatorId,
                "RenewalRejected",
                "Yêu cầu gia hạn của bạn đã bị từ chối.",
                relationship.Id,
                "CoachRequest",
                cancellationToken);
        }
        else
        {
            await notificationService.NotifyAsync(
                otherPartyId,
                "RenewalCancelled",
                $"{actorName} đã hủy yêu cầu gia hạn.",
                relationship.Id,
                "CoachRequest",
                cancellationToken);
        }

        return Unit.Value;
    }
}
