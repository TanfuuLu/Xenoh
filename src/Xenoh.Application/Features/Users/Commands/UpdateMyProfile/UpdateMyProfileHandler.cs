using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Coaches;
using Xenoh.Application.Features.Users.Queries.GetMyProfile;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Users.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileHandler(
    IWorkoutHistoryRepository workoutHistoryRepo,
    IBodyweightRepository bodyweightRepo,
    IUserPrRepository userPrRepo,
    IApplicationDbContext db,
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

        if (request.FirstName is not null)
        {
            var firstName = request.FirstName.Trim();
            if (string.IsNullOrWhiteSpace(firstName))
                throw new InvalidOperationException("First name is required.");

            user.FirstName = firstName;
        }

        if (request.LastName is not null)
        {
            var lastName = request.LastName.Trim();
            if (string.IsNullOrWhiteSpace(lastName))
                throw new InvalidOperationException("Last name is required.");

            user.LastName = lastName;
        }

        if (request.Bio is not null) user.Bio = request.Bio;
        if (request.Height is not null) user.Height = request.Height;
        if (request.Gender is not null) user.Gender = request.Gender;
        if (request.DateOfBirth is not null) user.DateOfBirth = request.DateOfBirth;
        if (request.FacebookUrl is not null) user.FacebookUrl = string.IsNullOrWhiteSpace(request.FacebookUrl) ? null : request.FacebookUrl.Trim();
        if (request.InstagramUrl is not null) user.InstagramUrl = string.IsNullOrWhiteSpace(request.InstagramUrl) ? null : request.InstagramUrl.Trim();
        if (request.ZaloUrl is not null) user.ZaloUrl = string.IsNullOrWhiteSpace(request.ZaloUrl) ? null : request.ZaloUrl.Trim();

        if (request.CoachMarketplaceProfile is not null && !await userManager.IsInRoleAsync(user, UserRole.Coach))
            throw new InvalidOperationException("Only coaches can update coach marketplace profile fields.");

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new InvalidOperationException(string.Join("; ", updateResult.Errors.Select(e => e.Description)));

        if (request.CoachMarketplaceProfile is not null)
        {
            await UpsertCoachMarketplaceProfileAsync(user.Id, request.CoachMarketplaceProfile, cancellationToken);
        }

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

        var marketplaceProfile = await db.CoachMarketplaceProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.CoachId == user.Id, cancellationToken);

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
            new Big3PrsResponse(squatPr, benchPr, deadliftPr),
            Math.Max(1, user.Level),
            user.TotalXp,
            Xenoh.Application.Common.XP.XpCalculator.XpToNextLevel(user.Level),
            Xenoh.Application.Common.XP.XpCalculator.GetTitle(user.Level),
            CoachMarketplaceProfileMapper.ToDto(marketplaceProfile),
            user.FacebookUrl,
            user.InstagramUrl,
            user.ZaloUrl
        );
    }

    private async Task UpsertCoachMarketplaceProfileAsync(
        Guid coachId,
        CoachMarketplaceProfileDto input,
        CancellationToken cancellationToken)
    {
        db.ChangeTracker.Clear();

        var profile = await db.CoachMarketplaceProfiles
            .FirstOrDefaultAsync(p => p.CoachId == coachId, cancellationToken);

        if (profile is null)
        {
            profile = new CoachMarketplaceProfile { CoachId = coachId };
            db.CoachMarketplaceProfiles.Add(profile);
        }

        profile.Headline = NormalizeText(input.Headline, 120, nameof(input.Headline));
        profile.ExperienceYears = NormalizeExperienceYears(input.ExperienceYears);
        profile.Specialties = NormalizeList(input.Specialties, nameof(input.Specialties));
        profile.Certifications = NormalizeList(input.Certifications, nameof(input.Certifications));
        profile.Languages = NormalizeList(input.Languages, nameof(input.Languages));
        profile.CoachingMethods = NormalizeList(input.CoachingMethods, nameof(input.CoachingMethods));
        profile.Achievements = NormalizeList(input.Achievements, nameof(input.Achievements));
        profile.ClientResultsSummary = NormalizeText(input.ClientResultsSummary, 500, nameof(input.ClientResultsSummary));
        profile.Availability = NormalizeText(input.Availability, 500, nameof(input.Availability));
        profile.ResponseTime = NormalizeText(input.ResponseTime, 500, nameof(input.ResponseTime));
        profile.CoachingStyle = NormalizeText(input.CoachingStyle, 500, nameof(input.CoachingStyle));
        profile.MonthlyPriceAmount = NormalizePrice(input.MonthlyPriceAmount, "Monthly price");
        profile.SessionPriceAmount = NormalizePrice(input.SessionPriceAmount, "Session price");
        profile.Currency = NormalizeText(input.Currency, 8, nameof(input.Currency))?.ToUpperInvariant() ?? "VND";
        profile.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string[] NormalizeList(string[]? values, string fieldName)
    {
        var normalized = (values ?? [])
            .Select(v => v.Trim())
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();

        if ((values ?? []).Count(v => !string.IsNullOrWhiteSpace(v)) > 8)
            throw new InvalidOperationException($"{fieldName} cannot exceed 8 items.");

        if (normalized.Any(v => v.Length > 60))
            throw new InvalidOperationException($"{fieldName} items cannot exceed 60 characters.");

        return normalized;
    }

    private static int? NormalizeExperienceYears(int? value)
    {
        if (value is null) return null;
        if (value is < 0 or > 60)
            throw new InvalidOperationException("Experience years must be between 0 and 60.");
        return value;
    }

    private static string? NormalizeText(string? value, int maxLength, string fieldName)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return null;
        if (trimmed.Length > maxLength)
            throw new InvalidOperationException($"{fieldName} cannot exceed {maxLength} characters.");
        return trimmed;
    }

    private static decimal? NormalizePrice(decimal? value, string fieldName)
    {
        if (value is null) return null;
        if (value < 0)
            throw new InvalidOperationException($"{fieldName} cannot be negative.");
        return value;
    }
}
