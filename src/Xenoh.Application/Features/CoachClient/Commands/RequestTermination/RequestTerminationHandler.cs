using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Commands.RequestTermination;

public sealed class RequestTerminationHandler(
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser,
    INotificationService notificationService,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<RequestTerminationCommand>
{
    public async ValueTask<Unit> Handle(RequestTerminationCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var relationship = await coachClientRepo.FindByIdForParticipantAsync(
            request.RelationshipId, userId, cancellationToken)
            ?? throw new InvalidOperationException("Relationship not found.");

        if (relationship.Status != RelationshipStatus.Active &&
            relationship.Status != RelationshipStatus.Expired &&
            relationship.Status != RelationshipStatus.PendingRenewal)
            throw new InvalidOperationException("Only active, expired or pending-renewal relationships can have a termination request.");

        var otherPartyId = userId == relationship.ClientId
            ? relationship.CoachId
            : relationship.ClientId;

        relationship.Status = RelationshipStatus.PendingTermination;
        relationship.TerminationRequestedBy = userId;
        relationship.RenewalRequestedBy = null;
        relationship.ProposedEndDate = null;
        relationship.UpdatedAt = DateTime.UtcNow;

        await coachClientRepo.SaveChangesAsync(cancellationToken);

        var initiator = await userManager.FindByIdAsync(userId.ToString());
        var initiatorName = initiator is null ? "Someone" : $"{initiator.FirstName} {initiator.LastName}";

        await notificationService.NotifyAsync(
            otherPartyId,
            "DisconnectRequested",
            $"{initiatorName} muốn ngắt kết nối với bạn.",
            relationship.Id,
            "CoachRequest",
            cancellationToken);

        return Unit.Value;
    }
}
