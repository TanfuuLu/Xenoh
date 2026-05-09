using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.ExerciseTemplates.Queries.GetLastExercisePerformance;

public sealed class GetLastExercisePerformanceHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<GetLastExercisePerformanceQuery, LastExercisePerformanceResponse>
{
    public async ValueTask<LastExercisePerformanceResponse> Handle(
        GetLastExercisePerformanceQuery request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        if (userId == Guid.Empty)
            throw new InvalidOperationException("User not authenticated.");

        var targetDay = await db.DailyWorkouts
            .AsNoTracking()
            .Where(d => d.Id == request.DailyWorkoutId &&
                        (d.WeeklyWorkout.Plan.OwnerId == userId ||
                         d.WeeklyWorkout.Plan.CreatedByCoachId == userId))
            .Select(d => new
            {
                d.Date,
                OwnerId = d.WeeklyWorkout.Plan.OwnerId
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Daily workout not found.");

        var lastSet = await db.ExerciseSets
            .AsNoTracking()
            .Where(s => s.IsCompleted &&
                        s.ActualWeight != null &&
                        s.Exercise.ExerciseTemplateId == request.ExerciseTemplateId &&
                        s.Exercise.DailyWorkout.WeeklyWorkout.Plan.OwnerId == targetDay.OwnerId &&
                        s.Exercise.DailyWorkout.Date < targetDay.Date)
            .OrderByDescending(s => s.CompletedAt != null)
            .ThenByDescending(s => s.CompletedAt)
            .ThenByDescending(s => s.Exercise.DailyWorkout.Date)
            .ThenByDescending(s => s.SetNumber)
            .Select(s => new
            {
                s.ActualWeight,
                s.ActualReps,
                s.Rpe,
                s.CompletedAt,
                WorkoutDate = s.Exercise.DailyWorkout.Date
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new LastExercisePerformanceResponse(
            request.ExerciseTemplateId,
            lastSet?.ActualWeight,
            lastSet?.ActualReps,
            lastSet?.Rpe,
            lastSet?.CompletedAt,
            lastSet?.WorkoutDate
        );
    }
}
