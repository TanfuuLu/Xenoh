using Mediator;
using Microsoft.AspNetCore.Identity;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Users.Queries.GetMyProfile;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Users.Commands.UpdateMyAvatar;

public sealed class UpdateMyAvatarHandler(
    IWorkoutHistoryRepository workoutHistoryRepo,
    IBodyweightRepository bodyweightRepo,
    IUserPrRepository userPrRepo,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser,
    IUserAvatarStorageService avatarStorage
) : IRequestHandler<UpdateMyAvatarCommand, UserProfileResponse>
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    public async ValueTask<UserProfileResponse> Handle(UpdateMyAvatarCommand request, CancellationToken cancellationToken)
    {
        if (request.Length <= 0)
            throw new InvalidOperationException("Avatar image is required.");

        if (request.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("Avatar image must be 5 MB or smaller.");

        if (!AllowedContentTypes.Contains(request.ContentType))
            throw new InvalidOperationException("Avatar image must be JPEG, PNG, WebP, or GIF.");

        var userId = currentUser.UserId;
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var avatarUrl = await avatarStorage.SaveAsync(
            userId,
            request.FileName,
            request.ContentType,
            request.Content,
            cancellationToken);

        user.AvatarUrl = avatarUrl;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new InvalidOperationException(string.Join("; ", updateResult.Errors.Select(e => e.Description)));

        var workoutDates = await workoutHistoryRepo.GetSortedDatesDescAsync(userId, cancellationToken);
        var currentStreak = GetMyProfileHandler.CalculateCurrentStreak(
            workoutDates, DateOnly.FromDateTime(DateTime.UtcNow));

        var latestWeight = await bodyweightRepo.GetLatestWeightAsync(userId, cancellationToken);
        var (bmi, bmiCategory) = GetMyProfileHandler.CalculateBmi(user.Height, latestWeight);

        var big3Prs = await userPrRepo.GetBig3Async(userId, cancellationToken);

        var gender = user.Gender.HasValue && Enum.IsDefined(user.Gender.Value) ? user.Gender : null;
        var dotsScore = GetMyProfileHandler.CalculateDots(gender, latestWeight, big3Prs);

        big3Prs.TryGetValue(CompetitionLiftType.Squat, out var squatPr);
        big3Prs.TryGetValue(CompetitionLiftType.Bench, out var benchPr);
        big3Prs.TryGetValue(CompetitionLiftType.Deadlift, out var deadliftPr);

        return new UserProfileResponse(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.AvatarUrl,
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
