using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Coaches.Queries.GetCoachProfile;

public sealed class GetCoachProfileHandler(
    ICoachClientRepository coachClientRepo,
    ICoachRatingRepository ratingRepo,
    IUserBlockRepository userBlockRepo,
    ICurrentUserService currentUser,
    IApplicationDbContext db,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<GetCoachProfileQuery, CoachProfileResponse>
{
    public async ValueTask<CoachProfileResponse> Handle(GetCoachProfileQuery request, CancellationToken cancellationToken)
    {
        var coach = await userManager.FindByIdAsync(request.CoachId.ToString())
            ?? throw new InvalidOperationException("Coach not found.");

        var isCoach = await userManager.IsInRoleAsync(coach, UserRole.Coach);
        if (!isCoach)
            throw new InvalidOperationException("Coach not found.");

        if (await userBlockRepo.IsEitherBlockedAsync(currentUser.UserId, request.CoachId, cancellationToken))
            throw new InvalidOperationException("Coach not found.");

        var totalClients = await coachClientRepo.CountActiveByCoachAsync(request.CoachId, cancellationToken);
        var ratingSummary = await ratingRepo.GetSummaryAsync(request.CoachId, currentUser.UserId, cancellationToken);
        var canRate = await ratingRepo.CanClientRateCoachAsync(request.CoachId, currentUser.UserId, cancellationToken);
        var ratings = await ratingRepo.GetByCoachAsync(request.CoachId, cancellationToken);
        var marketplaceProfile = await db.CoachMarketplaceProfiles
            .AsNoTracking()
            .Include(p => p.Packages)
            .FirstOrDefaultAsync(p => p.CoachId == request.CoachId, cancellationToken);

        return new CoachProfileResponse(
            coach.Id,
            $"{coach.FirstName} {coach.LastName}",
            coach.Email!,
            coach.AvatarUrl,
            coach.Bio,
            totalClients,
            ratingSummary.AverageRating,
            ratingSummary.RatingCount,
            canRate,
            ratingSummary.MyRating,
            ratings,
            CoachMarketplaceProfileMapper.ToDto(marketplaceProfile),
            CoachMarketplaceProfileMapper.ToPackageDtos(marketplaceProfile?.Packages)
        );
    }
}
