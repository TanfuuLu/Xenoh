using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Users.Queries.GetMyProfile;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Users.Queries.GetUserProfile;

public sealed class GetUserProfileHandler(
    IApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser
) : IRequestHandler<GetUserProfileQuery, UserProfileResponse>
{
    public async ValueTask<UserProfileResponse> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        var callerId = currentUser.UserId;

        // Authorization: caller must have an active coach–client relationship with the target user
        // (coach viewing client OR client viewing coach — either direction is valid)
        var hasRelationship = await context.CoachClientRelationships
            .AsNoTracking()
            .AnyAsync(r =>
                r.Status == RelationshipStatus.Active &&
                ((r.CoachId == callerId && r.ClientId == request.UserId) ||
                 (r.ClientId == callerId && r.CoachId == request.UserId)),
                cancellationToken);

        if (!hasRelationship)
            throw new UnauthorizedAccessException("You do not have access to this user's profile.");

        var user = await userManager.FindByIdAsync(request.UserId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var workoutDates = await context.WorkoutHistories
            .AsNoTracking()
            .Where(h => h.UserId == request.UserId)
            .OrderByDescending(h => h.Date)
            .Select(h => h.Date)
            .ToListAsync(cancellationToken);

        int currentStreak = GetMyProfileHandler.CalculateCurrentStreak(
            workoutDates, DateOnly.FromDateTime(DateTime.UtcNow));

        var latestLog = await context.BodyweightLogs
            .AsNoTracking()
            .Where(b => b.UserId == request.UserId)
            .OrderByDescending(b => b.Date)
            .Select(b => (decimal?)b.Weight)
            .FirstOrDefaultAsync(cancellationToken);

        var (bmi, bmiCategory) = GetMyProfileHandler.CalculateBmi(user.Height, latestLog);

        var big3Prs = await (
            from pr in context.UserExercisePRs.AsNoTracking()
            join t in context.ExerciseTemplates.AsNoTracking()
                on pr.ExerciseTemplateId equals t.Id
            where pr.UserId == request.UserId && t.IsCompetitionLift
            select new { t.CompetitionLiftType, Weight = (decimal?)pr.Weight }
        ).ToDictionaryAsync(x => x.CompetitionLiftType!.Value, x => x.Weight, cancellationToken);

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
