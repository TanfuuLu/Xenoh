using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Commands.RequestRenewal;

public sealed class RequestRenewalHandler(
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser,
    INotificationService notificationService,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<RequestRenewalCommand>
{
    public async ValueTask<Unit> Handle(RequestRenewalCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var relationship = await coachClientRepo.FindByIdForParticipantAsync(
            request.RelationshipId, userId, cancellationToken)
            ?? throw new InvalidOperationException("Relationship not found.");

        if (relationship.Status != RelationshipStatus.Active &&
            relationship.Status != RelationshipStatus.Expired)
            throw new InvalidOperationException("Renewal can only be requested from active or expired relationships.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var floor = relationship.EndDate is { } end && end > today ? end : today;
        if (request.ProposedEndDate <= floor)
            throw new InvalidOperationException("Proposed end date must be after the current end date.");

        var otherPartyId = userId == relationship.ClientId ? relationship.CoachId : relationship.ClientId;

        relationship.Status = RelationshipStatus.PendingRenewal;
        relationship.RenewalRequestedBy = userId;
        relationship.ProposedEndDate = request.ProposedEndDate;
        relationship.UpdatedAt = DateTime.UtcNow;

        await coachClientRepo.SaveChangesAsync(cancellationToken);

        var initiator = await userManager.FindByIdAsync(userId.ToString());
        var initiatorName = initiator is null ? "Đối tác" : $"{initiator.FirstName} {initiator.LastName}";

        await notificationService.NotifyAsync(
            otherPartyId,
            "RenewalRequested",
            $"{initiatorName} đề nghị gia hạn hợp đồng đến {request.ProposedEndDate:dd/MM/yyyy}.",
            relationship.Id,
            "CoachRequest",
            cancellationToken);

        return Unit.Value;
    }
}
