using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Users.Queries.GetMyProfile;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.Users.Queries.GetPublicUserProfile;

public sealed class GetPublicUserProfileHandler(
    IWorkoutHistoryRepository workoutHistoryRepo,
    IBodyweightRepository bodyweightRepo,
    IUserPrRepository userPrRepo,
    UserManager<ApplicationUser> userManager
) : IRequestHandler<GetPublicUserProfileQuery, PublicUserProfileResponse>
{
    public async ValueTask<PublicUserProfileResponse> Handle(GetPublicUserProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var workoutDates = await workoutHistoryRepo.GetSortedDatesDescAsync(request.UserId, cancellationToken);
        var currentStreak = GetMyProfileHandler.CalculateCurrentStreak(workoutDates, DateOnly.FromDateTime(DateTime.UtcNow));

        var latestBodyweight = await bodyweightRepo.GetLatestWeightAsync(request.UserId, cancellationToken);
        var (bmi, bmiCategory) = GetMyProfileHandler.CalculateBmi(user.Height, latestBodyweight);

        var big3Prs = await userPrRepo.GetBig3Async(request.UserId, cancellationToken);
        var gender = user.Gender.HasValue && Enum.IsDefined(user.Gender.Value) ? user.Gender : null;
        var dotsScore = GetMyProfileHandler.CalculateDots(gender, latestBodyweight, big3Prs);

        return new PublicUserProfileResponse(
            user.Id,
            $"{user.FirstName} {user.LastName}",
            user.Email!,
            user.AvatarUrl,
            user.Bio,
            gender.HasValue ? gender.Value.ToString() : null,
            user.Height,
            latestBodyweight,
            bmi,
            bmiCategory,
            currentStreak,
            dotsScore
        );
    }
}
