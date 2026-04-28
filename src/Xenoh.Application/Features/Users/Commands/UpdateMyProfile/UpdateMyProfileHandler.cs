using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Users.Queries.GetMyProfile;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Users.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileHandler(
    IWorkoutHistoryRepository workoutHistoryRepo,
    IBodyweightRepository bodyweightRepo,
    IUserPrRepository userPrRepo,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser
) : IRequestHandler<UpdateMyProfileCommand, UserProfileResponse>
{
    public async ValueTask<UserProfileResponse> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        if (request.DateOfBirth is not null
            && request.DateOfBirth.Value > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new InvalidOperationException("Date of birth cannot be in the future.");

        if (request.Bio is not null) user.Bio = request.Bio;
        if (request.Height is not null) user.Height = request.Height;
        if (request.Gender is not null) user.Gender = request.Gender;
        if (request.DateOfBirth is not null) user.DateOfBirth = request.DateOfBirth;

        await userManager.UpdateAsync(user);

        var workoutDates = await workoutHistoryRepo.GetSortedDatesDescAsync(userId, cancellationToken);
        int currentStreak = GetMyProfileHandler.CalculateCurrentStreak(
            workoutDates, DateOnly.FromDateTime(DateTime.UtcNow));

        var latestWeight = await bodyweightRepo.GetLatestWeightAsync(userId, cancellationToken);
        var (bmi, bmiCategory) = GetMyProfileHandler.CalculateBmi(user.Height, latestWeight);

        var big3Prs = await userPrRepo.GetBig3Async(userId, cancellationToken);

        var gender = user.Gender.HasValue && Enum.IsDefined(user.Gender.Value) ? user.Gender : null;
        var dotsScore = GetMyProfileHandler.CalculateDots(gender, latestWeight, big3Prs);

        big3Prs.TryGetValue(CompetitionLiftType.Squat,    out var squatPr);
        big3Prs.TryGetValue(CompetitionLiftType.Bench,    out var benchPr);
        big3Prs.TryGetValue(CompetitionLiftType.Deadlift, out var deadliftPr);

        return new UserProfileResponse(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.Bio,
            user.Height,
            gender.HasValue ? gender.Value.ToString() : null,
            user.DateOfBirth,
            currentStreak,
            latestWeight,
            bmi,
            bmiCategory,
            dotsScore,
            new Big3PrsResponse(squatPr, benchPr, deadliftPr)
        );
    }
}
