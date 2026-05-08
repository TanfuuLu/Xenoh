using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Commands.RequestCoach;

public sealed class RequestCoachHandler(
    ICoachClientRepository coachClientRepo,
    IUserBlockRepository userBlockRepo,
    ICurrentUserService currentUser,
    INotificationService notificationService,
    ISubscriptionService subscriptionService,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<RequestCoachCommand, CoachRelationshipResponse>
{
    public async ValueTask<CoachRelationshipResponse> Handle(RequestCoachCommand request, CancellationToken cancellationToken)
    {
        var clientId = currentUser.UserId;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (request.StartDate < today)
            throw new InvalidOperationException("Start date cannot be in the past.");
        if (request.EndDate <= request.StartDate)
            throw new InvalidOperationException("End date must be after start date.");

        if (await userBlockRepo.IsEitherBlockedAsync(clientId, request.CoachId, cancellationToken))
            throw new InvalidOperationException("Cannot connect with this user.");

        var existing = await coachClientRepo.FindByClientAsync(clientId, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException("You already have a coach or a pending request.");

        var coach = await userManager.FindByIdAsync(request.CoachId.ToString())
            ?? throw new InvalidOperationException("Coach not found.");

        var coachRoles = await userManager.GetRolesAsync(coach);
        if (!coachRoles.Contains(UserRole.Coach))
            throw new InvalidOperationException("The specified user is not a coach.");

        var maxClients = await subscriptionService.GetMaxClientsAsync(request.CoachId, cancellationToken);
        var currentClientCount = await coachClientRepo.CountActiveByCoachAsync(request.CoachId, cancellationToken);
        if (maxClients != int.MaxValue && currentClientCount >= maxClients)
            throw new InvalidOperationException("This coach has reached their client limit on their current subscription.");

        var client = await userManager.FindByIdAsync(clientId.ToString())
            ?? throw new InvalidOperationException("Client not found.");

        var relationship = new CoachClientRelationship
        {
            ClientId = clientId,
            CoachId = request.CoachId,
            Status = RelationshipStatus.Pending,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        await coachClientRepo.AddAsync(relationship, cancellationToken);
        await coachClientRepo.SaveChangesAsync(cancellationToken);

        await notificationService.NotifyAsync(
            request.CoachId,
            "CoachRequest",
            $"{client.FirstName} {client.LastName} đã gửi yêu cầu kết nối với bạn.",
            relationship.Id,
            "CoachRequest",
            cancellationToken);

        return new CoachRelationshipResponse(
            relationship.Id,
            clientId,
            $"{client.FirstName} {client.LastName}",
            client.AvatarUrl,
            request.CoachId,
            $"{coach.FirstName} {coach.LastName}",
            relationship.Status.ToString(),
            relationship.CreatedAt,
            null,
            relationship.StartDate,
            relationship.EndDate,
            null,
            null
        );
    }
}
