using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Features.Community;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.TrainingDayShares.Commands.ShareTrainingDay;

public sealed class ShareTrainingDayHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationService notifications
) : IRequestHandler<ShareTrainingDayCommand, TrainingDayShareResponse>
{
    public async ValueTask<TrainingDayShareResponse> Handle(
        ShareTrainingDayCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var day = await db.DailyWorkouts
            .Include(d => d.WeeklyWorkout)
                .ThenInclude(w => w.Plan)
            .Include(d => d.Exercises)
                .ThenInclude(e => e.Sets)
            .FirstOrDefaultAsync(d => d.Id == request.DailyWorkoutId, cancellationToken)
            ?? throw new InvalidOperationException("Daily workout not found.");

        if (day.WeeklyWorkout.Plan.OwnerId != userId)
            throw new InvalidOperationException("Access denied.");
        if (!day.IsCompleted)
            throw new InvalidOperationException("Only completed training days can be shared.");

        var alreadyShared = await db.TrainingDayShares
            .AsNoTracking()
            .AnyAsync(s => s.UserId == userId && s.SourceDailyWorkoutId == request.DailyWorkoutId, cancellationToken);
        if (alreadyShared)
            throw new InvalidOperationException("This training day has already been shared.");

        var caption = string.IsNullOrWhiteSpace(request.Caption) ? null : request.Caption.Trim();
        if (caption?.Length > 500)
            throw new InvalidOperationException("Caption cannot exceed 500 characters.");

        var performedExercises = day.Exercises
            .OrderBy(e => e.SortOrder)
            .ThenBy(e => e.CreatedAt)
            .ToList();

        var completedSets = performedExercises
            .SelectMany(e => e.Sets)
            .Where(s => s.IsCompleted)
            .ToList();
        var rpeValues = completedSets
            .Where(s => s.Rpe.HasValue)
            .Select(s => s.Rpe!.Value)
            .ToList();
        var exerciseTemplateIds = performedExercises
            .Select(e => e.ExerciseTemplateId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        var currentPrs = exerciseTemplateIds.Count == 0
            ? new Dictionary<Guid, decimal>()
            : await db.UserExercisePRs
                .AsNoTracking()
                .Where(pr => pr.UserId == userId && exerciseTemplateIds.Contains(pr.ExerciseTemplateId))
                .ToDictionaryAsync(pr => pr.ExerciseTemplateId, pr => pr.Weight, cancellationToken);

        bool IsPersonalRecordExercise(Exercise e) =>
            currentPrs.TryGetValue(e.ExerciseTemplateId, out var prWeight) &&
            e.Sets.Any(s => s.IsCompleted && (s.ActualWeight ?? s.PlannedWeight) == prWeight);

        var hasPersonalRecord = performedExercises.Any(IsPersonalRecordExercise);

        var share = new TrainingDayShare
        {
            UserId = userId,
            SourceDailyWorkoutId = day.Id,
            WorkoutDate = day.Date,
            DayOfWeek = day.DayOfWeek,
            DayStatus = day.Status,
            ExerciseCount = performedExercises.Count,
            CompletedSets = completedSets.Count,
            TotalVolume = completedSets.Sum(s => (s.ActualWeight ?? s.PlannedWeight ?? 0) * (s.ActualReps ?? s.PlannedReps)),
            TotalDurationSeconds = performedExercises.Sum(e => e.DurationSeconds ?? 0),
            AverageRpe = rpeValues.Count == 0 ? null : Math.Round(rpeValues.Average(), 2),
            HasPersonalRecord = hasPersonalRecord,
            Caption = caption,
            Exercises = performedExercises.Select(e => new TrainingDayShareExercise
            {
                Name = e.Name,
                PrimaryMuscleGroup = e.PrimaryMuscleGroup,
                ExerciseKind = e.ExerciseKind,
                SortOrder = e.SortOrder,
                IsSkipped = e.IsSkipped,
                IsPersonalRecord = IsPersonalRecordExercise(e),
                DurationSeconds = e.DurationSeconds,
                Notes = e.Notes,
                Sets = e.Sets
                    .OrderBy(s => s.SetNumber)
                    .Select(s => new TrainingDayShareSet
                    {
                        SetNumber = s.SetNumber,
                        ActualReps = s.ActualReps,
                        ActualWeight = s.ActualWeight,
                        Rpe = s.Rpe,
                        IsCompleted = s.IsCompleted
                    })
                    .ToList()
            }).ToList()
        };

        db.TrainingDayShares.Add(share);
        await db.SaveChangesAsync(cancellationToken);

        var current = await db.ApplicationUsers.AsNoTracking().FirstAsync(u => u.Id == userId, cancellationToken);
        var friendIds = await db.Friendships
            .AsNoTracking()
            .Where(f => f.Status == FriendshipStatus.Accepted && (f.UserAId == userId || f.UserBId == userId))
            .Select(f => f.UserAId == userId ? f.UserBId : f.UserAId)
            .ToListAsync(cancellationToken);

        foreach (var friendId in friendIds)
        {
            await notifications.NotifyAsync(
                friendId,
                "TrainingDayShared",
                $"{CommunityMapping.FullName(current)} shared a training day.",
                share.Id,
                "TrainingDayShare",
                cancellationToken);
        }

        share.User = current;
        return TrainingDayShareMapping.ToResponse(share, userId);
    }
}
