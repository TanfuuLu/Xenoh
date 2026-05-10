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

        var updatesCoachMarketplace = request.CoachMarketplaceProfile is not null || request.CoachPackages is not null;
        if (updatesCoachMarketplace && !await userManager.IsInRoleAsync(user, UserRole.Coach))
            throw new InvalidOperationException("Only coaches can update coach marketplace profile fields.");

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            throw new InvalidOperationException(string.Join("; ", updateResult.Errors.Select(e => e.Description)));

        if (updatesCoachMarketplace)
        {
            await UpsertCoachMarketplaceProfileAsync(user.Id, request, cancellationToken);
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
            .Include(p => p.Packages)
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
            CoachMarketplaceProfileMapper.ToPackageDtos(marketplaceProfile?.Packages)
        );
    }

    private async Task UpsertCoachMarketplaceProfileAsync(
        Guid coachId,
        UpdateMyProfileCommand request,
        CancellationToken cancellationToken)
    {
        // Clear stale entities tracked by UserManager.UpdateAsync (ApplicationUser has a
        // ConcurrencyStamp token; leaving it in the tracker causes a phantom UPDATE that
        // finds 0 rows and throws DbUpdateConcurrencyException).
        db.ChangeTracker.Clear();

        var profile = await db.CoachMarketplaceProfiles
            .FirstOrDefaultAsync(p => p.CoachId == coachId, cancellationToken);

        if (profile is null)
        {
            profile = new CoachMarketplaceProfile { CoachId = coachId };
            db.CoachMarketplaceProfiles.Add(profile);
        }

        if (request.CoachMarketplaceProfile is { } input)
        {
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
        }

        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken); // persist profile before FK operations

        if (request.CoachPackages is { } packages)
        {
            if (packages.Count > 4)
                throw new InvalidOperationException("Coach packages cannot exceed 4 items.");

            // Direct SQL DELETE — avoids change-tracker state issues entirely.
            await db.CoachPackages
                .Where(p => p.CoachMarketplaceProfileId == profile.Id)
                .ExecuteDeleteAsync(cancellationToken);

            foreach (var (p, index) in packages.OrderBy(p => p.DisplayOrder).Take(4).Select((p, i) => (p, i)))
            {
                var name = NormalizeRequiredText(p.Name, 80, "Package name");
                var duration = NormalizeRequiredText(p.DurationLabel, 80, "Package duration");
                var description = NormalizeText(p.Description, 300, "Package description");
                var currency = NormalizeText(p.Currency, 8, "Package currency") ?? "VND";

                if (p.PriceAmount is < 0)
                    throw new InvalidOperationException("Package price cannot be negative.");

                var type = NormalizeText(p.Type, 80, "Package type");

                db.CoachPackages.Add(new CoachPackage
                {
                    CoachMarketplaceProfileId = profile.Id,
                    Name = name,
                    PriceAmount = p.PriceAmount,
                    Currency = currency.ToUpperInvariant(),
                    DurationLabel = duration,
                    Description = description,
                    Type = type,
                    DisplayOrder = index
                });
            }

            await db.SaveChangesAsync(cancellationToken);
        }
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

    private static string NormalizeRequiredText(string value, int maxLength, string fieldName)
    {
        var trimmed = value.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
            throw new InvalidOperationException($"{fieldName} is required.");
        if (trimmed.Length > maxLength)
            throw new InvalidOperationException($"{fieldName} cannot exceed {maxLength} characters.");
        return trimmed;
    }
}
