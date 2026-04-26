using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Users.Queries.GetMyProfile;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Users.Queries.GetUserProfile;

public sealed class GetUserProfileHandler(
    ICoachClientRepository coachClientRepo,
    IWorkoutHistoryRepository workoutHistoryRepo,
    IBodyweightRepository bodyweightRepo,
    IUserPrRepository userPrRepo,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser
) : IRequestHandler<GetUserProfileQuery, UserProfileResponse>
{
    public async ValueTask<UserProfileResponse> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var callerId = currentUser.UserId;

        var hasRelationship = await coachClientRepo.HasActiveRelationshipAsync(
            callerId, request.UserId, cancellationToken);

        if (!hasRelationship)
            throw new UnauthorizedAccessException("You do not have access to this user's profile.");

        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var workoutDates = await workoutHistoryRepo.GetSortedDatesDescAsync(request.UserId, cancellationToken);
        int currentStreak = GetMyProfileHandler.CalculateCurrentStreak(
            workoutDates, DateOnly.FromDateTime(DateTime.UtcNow));

        var latestLog = await bodyweightRepo.GetLatestWeightAsync(request.UserId, cancellationToken);
        var (bmi, bmiCategory) = GetMyProfileHandler.CalculateBmi(user.Height, latestLog);

        var big3Prs = await userPrRepo.GetBig3Async(request.UserId, cancellationToken);

        var gender = user.Gender.HasValue && Enum.IsDefined(user.Gender.Value)
            ? user.Gender
            : null;
        var dotsScore = GetMyProfileHandler.CalculateDots(gender, latestLog, big3Prs);

        big3Prs.TryGetValue(CompetitionLiftType.Squat,    out var squatPr);
        big3Prs.TryGetValue(CompetitionLiftType.Bench,    out var benchPr);
        big3Prs.TryGetValue(CompetitionLiftType.Deadlift, out var deadliftPr);

        return new UserProfileResponse(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.Height,
            gender.HasValue ? gender.Value.ToString() : null,
            user.DateOfBirth,
            currentStreak,
            latestLog,
            bmi,
            bmiCategory,
            dotsScore,
            new Big3PrsResponse(squatPr, benchPr, deadliftPr)
        );
    }
}
