using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.CoachClient;
using Xenoh.Application.Features.CoachClient.Commands.RequestCoach;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Commands.AcceptRequest;

public sealed class AcceptRequestHandler(
    ICoachClientRepository coachClientRepo,
    ICurrentUserService currentUser,
    INotificationService notificationService,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<AcceptRequestCommand, CoachRelationshipResponse>
{
    public async ValueTask<CoachRelationshipResponse> Handle(AcceptRequestCommand request, CancellationToken cancellationToken)
    {
        var coachId = currentUser.UserId;

        var relationship = await coachClientRepo.FindByIdForCoachAsync(request.RelationshipId, coachId, cancellationToken)
            ?? throw new InvalidOperationException("Request not found.");

        if (relationship.Status != RelationshipStatus.Pending)
            throw new InvalidOperationException("Request has already been processed.");

        relationship.Status = RelationshipStatus.Active;
        relationship.UpdatedAt = DateTime.UtcNow;

        await coachClientRepo.SaveChangesAsync(cancellationToken);

        var coach = await userManager.FindByIdAsync(coachId.ToString());

        await notificationService.NotifyAsync(
            relationship.ClientId,
            "CoachAccepted",
            $"Coach {coach!.FirstName} {coach.LastName} đã chấp nhận yêu cầu kết nối của bạn.",
            relationship.Id,
            "CoachRequest",
            cancellationToken);

        return CoachRelationshipMapper.ToResponse(relationship, relationship.Client, coach!);
    }
}
