using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Commands.AcceptTermination;

public sealed class AcceptTerminationHandler(
    ICoachClientRepository coachClientRepo,
    IPlanRepository planRepo,
    ICurrentUserService currentUser,
    INotificationService notificationService
) : IRequestHandler<AcceptTerminationCommand>
{
    public async ValueTask<Unit> Handle(AcceptTerminationCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var relationship = await coachClientRepo.FindByIdForParticipantAsync(
            request.RelationshipId, userId, cancellationToken)
            ?? throw new InvalidOperationException("Relationship not found.");

        if (relationship.Status != RelationshipStatus.PendingTermination)
            throw new InvalidOperationException("No pending termination request for this relationship.");

        if (relationship.TerminationRequestedBy == userId)
            throw new InvalidOperationException("You cannot accept your own termination request. Use reject to cancel it.");

        var initiatorId = relationship.TerminationRequestedBy!.Value;

        await planRepo.DeleteCoachPlansForClientAsync(
            relationship.ClientId, relationship.CoachId, cancellationToken);

        relationship.Status = RelationshipStatus.Ended;
        relationship.TerminationRequestedBy = null;
        relationship.UpdatedAt = DateTime.UtcNow;

        await coachClientRepo.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyAsync(
            initiatorId,
            "DisconnectAccepted",
            "Yêu cầu ngắt kết nối của bạn đã được chấp nhận.",
            relationship.Id,
            "CoachRequest",
            cancellationToken);

        return Unit.Value;
    }
}
