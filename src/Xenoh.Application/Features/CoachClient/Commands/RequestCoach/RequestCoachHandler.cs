using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.CoachClient;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.CoachClient.Commands.RequestCoach;

public sealed class RequestCoachHandler(
    ICoachClientRepository coachClientRepo,
    IUserBlockRepository userBlockRepo,
    ICurrentUserService currentUser,
    INotificationService notificationService,
    ISubscriptionService subscriptionService,
    IApplicationDbContext db,
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
        if (!Enum.TryParse<CoachingType>(request.CoachingType, ignoreCase: true, out var selectedCoachingType))
            throw new InvalidOperationException("Invalid coaching type.");
        if (request.Quantity is < 1 or > 120)
            throw new InvalidOperationException("Quantity must be between 1 and 120.");

        var expectedEndDate = selectedCoachingType switch
        {
            CoachingType.Monthly => request.StartDate.AddMonths(request.Quantity),
            CoachingType.Session => request.StartDate.AddDays(request.Quantity),
            _ => throw new InvalidOperationException("Invalid coaching type.")
        };
        if (request.EndDate != expectedEndDate)
            throw new InvalidOperationException("End date must match the selected coaching quantity.");

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

        var marketplaceProfile = await db.CoachMarketplaceProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.CoachId == request.CoachId, cancellationToken)
            ?? throw new InvalidOperationException("This coach has not configured pricing yet.");

        var selectedPrice = selectedCoachingType switch
        {
            CoachingType.Monthly => marketplaceProfile.MonthlyPriceAmount,
            CoachingType.Session => marketplaceProfile.SessionPriceAmount,
            _ => null
        };

        if (selectedPrice is null)
            throw new InvalidOperationException("This coach has not configured pricing for the selected coaching type.");
        if (selectedPrice < 0)
            throw new InvalidOperationException("Selected coaching price is invalid.");

        var maxClients = await subscriptionService.GetMaxClientsAsync(request.CoachId, cancellationToken);
        var overlappingCount = await coachClientRepo.CountOverlappingActiveByCoachAsync(
            request.CoachId, request.StartDate, request.EndDate, cancellationToken);
        if (maxClients != int.MaxValue && overlappingCount >= maxClients)
            throw new InvalidOperationException(
                "This coach is fully booked during your requested contract period.");

        var client = await userManager.FindByIdAsync(clientId.ToString())
            ?? throw new InvalidOperationException("Client not found.");

        var relationship = new CoachClientRelationship
        {
            ClientId = clientId,
            CoachId = request.CoachId,
            Status = RelationshipStatus.Pending,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            SelectedCoachingType = selectedCoachingType,
            SelectedQuantity = request.Quantity,
            SelectedPriceAmount = selectedPrice,
            SelectedCurrency = marketplaceProfile.Currency
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

        return CoachRelationshipMapper.ToResponse(relationship, client, coach);
    }
}
