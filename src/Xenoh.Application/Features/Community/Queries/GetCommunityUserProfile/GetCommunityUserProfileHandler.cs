using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Users.Queries.GetMyProfile;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Community.Queries.GetCommunityUserProfile;

public sealed class GetCommunityUserProfileHandler(
    IApplicationDbContext db,
    IWorkoutHistoryRepository workoutHistoryRepo,
    IBodyweightRepository bodyweightRepo,
    IUserPrRepository userPrRepo,
    ICurrentUserService currentUser
) : IRequestHandler<GetCommunityUserProfileQuery, CommunityUserProfileResponse>
{
    public async ValueTask<CommunityUserProfileResponse> Handle(
        GetCommunityUserProfileQuery request,
        CancellationToken cancellationToken)
    {
        var currentUserId = currentUser.UserId;
        var target = await db.ApplicationUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new InvalidOperationException("User not found.");

        var isBlocked = await db.UserBlocks
            .AsNoTracking()
            .AnyAsync(b =>
                (b.BlockerId == currentUserId && b.BlockedId == request.UserId) ||
                (b.BlockerId == request.UserId && b.BlockedId == currentUserId),
                cancellationToken);
        if (isBlocked)
            throw new InvalidOperationException("User not found.");

        var friendship = await db.Friendships
            .AsNoTracking()
            .FirstOrDefaultAsync(f =>
                (f.UserAId == currentUserId && f.UserBId == request.UserId) ||
                (f.UserAId == request.UserId && f.UserBId == currentUserId),
                cancellationToken);

        var canViewStats = currentUserId == request.UserId || friendship?.Status == FriendshipStatus.Accepted;
        decimal? latestBodyweight = null;
        decimal? bmi = null;
        string? bmiCategory = null;
        int? currentStreak = null;
        decimal? dotsScore = null;
        long totalTrainingDurationSeconds = 0;
        decimal totalTrainingVolume = 0;
        decimal? squatPr = null;
        decimal? benchPr = null;
        decimal? deadliftPr = null;
        decimal? big3Total = null;

        if (canViewStats)
        {
            var workoutDates = await workoutHistoryRepo.GetSortedDatesDescAsync(request.UserId, cancellationToken);
            currentStreak = GetMyProfileHandler.CalculateCurrentStreak(workoutDates, DateOnly.FromDateTime(DateTime.UtcNow));
            latestBodyweight = await bodyweightRepo.GetLatestWeightAsync(request.UserId, cancellationToken);
            (bmi, bmiCategory) = GetMyProfileHandler.CalculateBmi(target.Height, latestBodyweight);
            var big3Prs = await userPrRepo.GetBig3Async(request.UserId, cancellationToken);
            dotsScore = GetMyProfileHandler.CalculateDots(target.Gender, latestBodyweight, big3Prs);
            big3Prs.TryGetValue(CompetitionLiftType.Squat, out squatPr);
            big3Prs.TryGetValue(CompetitionLiftType.Bench, out benchPr);
            big3Prs.TryGetValue(CompetitionLiftType.Deadlift, out deadliftPr);
            big3Total = new[] { squatPr, benchPr, deadliftPr }
                .Where(value => value.HasValue)
                .Sum(value => value!.Value);
            if (big3Total == 0)
                big3Total = null;

            totalTrainingDurationSeconds = await db.Exercises
                .AsNoTracking()
                .Where(e =>
                    e.IsCompleted &&
                    !e.IsSkipped &&
                    e.DailyWorkout.IsCompleted &&
                    e.DailyWorkout.WeeklyWorkout.Plan.OwnerId == request.UserId)
                .SumAsync(e => (int?)(e.DurationSeconds ?? 0), cancellationToken) ?? 0;

            totalTrainingVolume = await db.ExerciseSets
                .AsNoTracking()
                .Where(s =>
                    s.IsCompleted &&
                    s.Exercise.DailyWorkout.IsCompleted &&
                    s.Exercise.DailyWorkout.WeeklyWorkout.Plan.OwnerId == request.UserId)
                .SumAsync(
                    s => ((s.ActualWeight ?? s.PlannedWeight) ?? 0) * (s.ActualReps ?? s.PlannedReps),
                    cancellationToken);
        }

        return new CommunityUserProfileResponse(
            target.Id,
            CommunityMapping.FullName(target),
            canViewStats ? target.Email : null,
            target.AvatarUrl,
            target.Bio,
            target.Gender?.ToString(),
            target.DevelopmentDirection?.ToString(),
            target.TrainingDiscipline?.ToString(),
            friendship?.Status.ToString() ?? "None",
            friendship?.Id,
            friendship is null ? null : CommunityMapping.RequestDirection(friendship, currentUserId),
            canViewStats,
            canViewStats ? target.Height : null,
            latestBodyweight,
            bmi,
            bmiCategory,
            currentStreak,
            dotsScore,
            totalTrainingDurationSeconds,
            totalTrainingVolume,
            new CommunityBig3PrsResponse(squatPr, benchPr, deadliftPr),
            big3Total,
            target.Level);
    }
}
