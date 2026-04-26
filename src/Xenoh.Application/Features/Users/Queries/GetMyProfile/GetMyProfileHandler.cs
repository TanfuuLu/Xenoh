using Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Users.Queries.GetMyProfile;

public sealed class GetMyProfileHandler(
    IApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUser
) : IRequestHandler<GetMyProfileQuery, UserProfileResponse>
{
    public async ValueTask<UserProfileResponse> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        // Streak: load workout history sorted descending
        var workoutDates = await context.WorkoutHistories
            .AsNoTracking()
            .Where(h => h.UserId == userId)
            .OrderByDescending(h => h.Date)
            .Select(h => h.Date)
            .ToListAsync(cancellationToken);

        int currentStreak = CalculateCurrentStreak(workoutDates, DateOnly.FromDateTime(DateTime.UtcNow));

        // Latest bodyweight (last 1 entry)
        var latestLog = await context.BodyweightLogs
            .AsNoTracking()
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.Date)
            .Select(b => (decimal?)b.Weight)
            .FirstOrDefaultAsync(cancellationToken);

        var (bmi, bmiCategory) = CalculateBmi(user.Height, latestLog);

        // DOTS Score: join UserExercisePRs with ExerciseTemplates to get Big 3 PRs
        var big3Prs = await (
            from pr in context.UserExercisePRs.AsNoTracking()
            join t in context.ExerciseTemplates.AsNoTracking()
                on pr.ExerciseTemplateId equals t.Id
            where pr.UserId == userId && t.IsCompetitionLift
            select new { t.CompetitionLiftType, Weight = (decimal?)pr.Weight }
        ).ToDictionaryAsync(x => x.CompetitionLiftType!.Value, x => x.Weight, cancellationToken);

        var gender = user.Gender.HasValue && Enum.IsDefined(user.Gender.Value)
            ? user.Gender
            : null;
        var dotsScore = CalculateDots(gender, latestLog, big3Prs);

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

    internal static int CalculateCurrentStreak(List<DateOnly> sortedDatesDesc, DateOnly today)
    {
        if (sortedDatesDesc.Count == 0) return 0;

        // Grace period: most recent workout must be within 3 days to have active streak
        if (today.DayNumber - sortedDatesDesc[0].DayNumber > 3) return 0;

        int streak = 1;
        for (int i = 1; i < sortedDatesDesc.Count; i++)
        {
            int gap = sortedDatesDesc[i - 1].DayNumber - sortedDatesDesc[i].DayNumber;
            if (gap <= 3)
                streak++;
            else
                break;
        }
        return streak;
    }

    internal static decimal? CalculateDots(
        Gender? gender,
        decimal? bodyweightKg,
        Dictionary<CompetitionLiftType, decimal?> big3Prs)
    {
        if (gender is null || bodyweightKg is null or <= 0)
            return null;

        // Require all 3 lifts
        if (!big3Prs.TryGetValue(CompetitionLiftType.Squat, out var squat) || squat is null)
            return null;
        if (!big3Prs.TryGetValue(CompetitionLiftType.Bench, out var bench) || bench is null)
            return null;
        if (!big3Prs.TryGetValue(CompetitionLiftType.Deadlift, out var deadlift) || deadlift is null)
            return null;

        var total = (double)(squat.Value + bench.Value + deadlift.Value);
        var bw = Math.Clamp((double)bodyweightKg.Value, 40.0, 210.0);

        // IPF DOTS coefficients (ascending polynomial: a0 + a1*w + a2*w² + a3*w³ + a4*w⁴ + a5*w⁵)
        double dots;
        if (gender == Gender.Male)
        {
            double f = 47.46178854
                + 8.472061379 * bw
                + 0.07369501861 * bw * bw
                + (-0.001395833811) * bw * bw * bw
                + 7.07665973070743e-6 * bw * bw * bw * bw
                + (-1.20804336482315e-8) * bw * bw * bw * bw * bw;
            dots = total * 500.0 / f;
        }
        else
        {
            double f = -125.4255398
                + 13.71219419 * bw
                + (-0.03307250631) * bw * bw
                + (-0.001050400051) * bw * bw * bw
                + 9.38773881462799e-6 * bw * bw * bw * bw
                + (-2.3334613884954e-8) * bw * bw * bw * bw * bw;
            dots = total * 500.0 / f;
        }

        return Math.Round((decimal)dots, 2);
    }

    internal static (decimal? Bmi, string? Category) CalculateBmi(decimal? heightCm, decimal? weightKg)
    {
        if (heightCm is null or <= 0 || weightKg is null or <= 0)
            return (null, null);

        var heightM = heightCm.Value / 100m;
        var bmi = Math.Round(weightKg.Value / (heightM * heightM), 1);
        var category = bmi switch
        {
            < 18.5m => "Underweight",
            >= 18.5m and < 25m => "Normal",
            >= 25m and < 30m => "Overweight",
            _ => "Obese"
        };
        return (bmi, category);
    }
}
