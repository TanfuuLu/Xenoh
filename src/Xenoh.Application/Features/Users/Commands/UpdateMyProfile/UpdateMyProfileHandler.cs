using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Users.Queries.GetMyProfile;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Users.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileHandler(
    IApplicationDbContext context,
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

        if (request.Height is not null) user.Height = request.Height;
        if (request.Gender is not null) user.Gender = request.Gender;
        if (request.DateOfBirth is not null) user.DateOfBirth = request.DateOfBirth;

        await userManager.UpdateAsync(user);

        // Return updated profile
        var workoutDates = await context.WorkoutHistories
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.Date)
            .Select(h => h.Date)
            .ToListAsync(cancellationToken);

        int currentStreak = GetMyProfileHandler.CalculateCurrentStreak(
            workoutDates, DateOnly.FromDateTime(DateTime.UtcNow));

        var latestWeight = await context.BodyweightLogs
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.Date)
            .Select(b => (decimal?)b.Weight)
            .FirstOrDefaultAsync(cancellationToken);

        var (bmi, bmiCategory) = GetMyProfileHandler.CalculateBmi(user.Height, latestWeight);

        // DOTS Score
        var big3Prs = await (
            from pr in context.UserExercisePRs.AsNoTracking()
            join t in context.ExerciseTemplates.AsNoTracking()
                on pr.ExerciseTemplateId equals t.Id
            where pr.UserId == userId && t.IsCompetitionLift
            select new { t.CompetitionLiftType, Weight = (decimal?)pr.Weight }
        ).ToDictionaryAsync(x => x.CompetitionLiftType!.Value, x => x.Weight, cancellationToken);

        var gender = user.Gender.HasValue && Enum.IsDefined(user.Gender.Value) ? user.Gender : null;
        var dotsScore = GetMyProfileHandler.CalculateDots(gender, latestWeight, big3Prs);

        return new UserProfileResponse(
            user.Id,
            user.Email!,
            user.FirstName,
            user.LastName,
            user.Height,
            gender.HasValue ? gender.Value.ToString() : null,
            user.DateOfBirth,
            currentStreak,
            latestWeight,
            bmi,
            bmiCategory,
            dotsScore
        );
    }
}
