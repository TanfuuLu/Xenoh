using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Commands.AcceptRenewal;

public sealed class AcceptRenewalHandler(
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser,
    INotificationService notificationService
) : IRequestHandler<AcceptRenewalCommand>
{
    public async ValueTask<Unit> Handle(AcceptRenewalCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var relationship = await coachClientRepo.FindByIdForParticipantAsync(
            request.RelationshipId, userId, cancellationToken)
            ?? throw new InvalidOperationException("Relationship not found.");

        if (relationship.Status != RelationshipStatus.PendingRenewal)
            throw new InvalidOperationException("No pending renewal for this relationship.");

        if (relationship.RenewalRequestedBy == userId)
            throw new InvalidOperationException("You cannot accept your own renewal request.");

        if (relationship.ProposedEndDate is null)
            throw new InvalidOperationException("Renewal has no proposed end date.");

        var initiatorId = relationship.RenewalRequestedBy!.Value;

        relationship.EndDate = relationship.ProposedEndDate;
        relationship.Status = RelationshipStatus.Active;
        relationship.RenewalRequestedBy = null;
        relationship.ProposedEndDate = null;
        relationship.UpdatedAt = DateTime.UtcNow;

        await coachClientRepo.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyAsync(
            initiatorId,
            "RenewalAccepted",
            $"Yêu cầu gia hạn của bạn đã được chấp nhận. Hợp đồng đến {relationship.EndDate:dd/MM/yyyy}.",
            relationship.Id,
            "CoachRequest",
            cancellationToken);

        return Unit.Value;
    }
}
