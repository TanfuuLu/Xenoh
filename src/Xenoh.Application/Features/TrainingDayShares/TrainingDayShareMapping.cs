using Xenoh.Application.Features.Community;
using Xenoh.Domain.Entities;

namespace Xenoh.Application.Features.TrainingDayShares;

internal static class TrainingDayShareMapping
{
    public static TrainingDayShareResponse ToResponse(TrainingDayShare share, Guid? currentUserId = null)
    {
        var loveCount = share.Loves.Count;
        var lovedByCurrentUser = currentUserId.HasValue && share.Loves.Any(l => l.UserId == currentUserId.Value);

        return new TrainingDayShareResponse(
            share.Id,
            share.UserId,
            CommunityMapping.FullName(share.User),
            share.User.AvatarUrl,
            share.SourceDailyWorkoutId,
            share.WorkoutDate,
            share.DayOfWeek.ToString(),
            share.DayStatus.ToString(),
            share.ExerciseCount,
            share.CompletedSets,
            share.TotalVolume,
            share.TotalDurationSeconds,
            share.AverageRpe,
            share.HasPersonalRecord,
            share.Caption,
            loveCount,
            lovedByCurrentUser,
            share.CreatedAt,
            share.Exercises
                .OrderBy(e => e.SortOrder)
                .Select(e => new TrainingDayShareExerciseResponse(
                    e.Id,
                    e.Name,
                    e.PrimaryMuscleGroup.ToString(),
                    e.ExerciseKind.ToString(),
                    e.SortOrder,
                    e.IsSkipped,
                    e.IsPersonalRecord,
                    e.DurationSeconds,
                    e.Notes,
                    e.Sets
                        .OrderBy(s => s.SetNumber)
                        .Select(s => new TrainingDayShareSetResponse(
                            s.Id,
                            s.SetNumber,
                            s.ActualReps,
                            s.ActualWeight,
                            s.Rpe,
                            s.IsCompleted))
                        .ToList()))
                .ToList());
    }
}
