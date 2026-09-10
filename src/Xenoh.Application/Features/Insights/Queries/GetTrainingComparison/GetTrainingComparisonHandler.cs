using Mediator;
using Microsoft.EntityFrameworkCore;
using Xenoh.Application.Common.Analytics;
using Xenoh.Application.Common.Interfaces;

namespace Xenoh.Application.Features.Insights.Queries.GetTrainingComparison;

public sealed class GetTrainingComparisonHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser
) : IRequestHandler<GetTrainingComparisonQuery, TrainingComparisonResponse>
{
    public async ValueTask<TrainingComparisonResponse> Handle(
        GetTrainingComparisonQuery request,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId == Guid.Empty)
            throw new InvalidOperationException("User not authenticated.");
        if (request.Days is not (7 or 28 or 90))
            throw new InvalidOperationException("Comparison period must be 7, 28, or 90 days.");

        var userId = currentUser.UserId;
        var asOf = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentStart = asOf.AddDays(1 - request.Days);
        var previousStart = currentStart.AddDays(-request.Days);

        var lifts = await db.ExerciseTemplates
            .AsNoTracking()
            .Where(template => db.Exercises.Any(exercise =>
                exercise.ExerciseTemplateId == template.Id &&
                exercise.DailyWorkout.WeeklyWorkout.Plan.OwnerId == userId &&
                exercise.DailyWorkout.Date <= asOf))
            .OrderBy(lift => lift.Name)
            .Select(template => new TrainingComparisonLift(template.Id, template.Name))
            .ToListAsync(cancellationToken);

        var selectedLift = request.ExerciseTemplateId is null
            ? null
            : lifts.SingleOrDefault(lift => lift.Id == request.ExerciseTemplateId.Value);
        if (request.ExerciseTemplateId is not null && selectedLift is null)
            throw new InvalidOperationException("The selected lift has no training history.");

        var rawSets = await db.ExerciseSets
            .AsNoTracking()
            .Where(set =>
                set.Exercise.DailyWorkout.WeeklyWorkout.Plan.OwnerId == userId &&
                set.Exercise.DailyWorkout.Date >= previousStart &&
                set.Exercise.DailyWorkout.Date <= asOf &&
                (request.ExerciseTemplateId == null || set.Exercise.ExerciseTemplateId == request.ExerciseTemplateId))
            .Select(set => new RawSet(
                set.Exercise.DailyWorkoutId,
                set.Exercise.DailyWorkout.Date,
                set.Exercise.ExerciseTemplateId,
                set.Exercise.Name,
                set.IsCompleted,
                set.PlannedReps,
                set.ActualReps,
                set.ActualWeight,
                set.Rpe))
            .ToListAsync(cancellationToken);

        var comparableSets = rawSets
            .Select(set => new TrainingComparisonSet(
                set.Date,
                set.IsCompleted,
                set.PlannedReps,
                set.ActualReps,
                set.ActualWeight,
                set.Rpe))
            .ToList();
        var result = TrainingComparisonAnalyzer.Analyze(asOf, request.Days, comparableSets);
        var sessions = rawSets
            .GroupBy(set => new { set.DailyWorkoutId, set.Date, set.ExerciseTemplateId, set.ExerciseName })
            .Select(group =>
            {
                var summary = TrainingComparisonAnalyzer.Summarize(
                    group.Key.Date,
                    group.Key.Date,
                    group.Select(set => new TrainingComparisonSet(
                        set.Date,
                        set.IsCompleted,
                        set.PlannedReps,
                        set.ActualReps,
                        set.ActualWeight,
                        set.Rpe)).ToList());
                return new TrainingComparisonSession(
                    group.Key.DailyWorkoutId,
                    group.Key.Date,
                    group.Key.ExerciseTemplateId,
                    group.Key.ExerciseName,
                    summary.CompletedSetCount,
                    summary.RepCompletionPercent,
                    summary.AverageRpe,
                    summary.BestEstimatedOneRepMax);
            })
            .OrderByDescending(session => session.Date)
            .Take(50)
            .ToList();

        return new TrainingComparisonResponse(
            new TrainingComparisonScope(
                request.Days,
                asOf,
                currentStart,
                previousStart,
                selectedLift?.Id,
                selectedLift?.Name),
            result.Current,
            result.Previous,
            result.Deltas,
            lifts,
            sessions);
    }

    private sealed record RawSet(
        Guid DailyWorkoutId,
        DateOnly Date,
        Guid ExerciseTemplateId,
        string ExerciseName,
        bool IsCompleted,
        int PlannedReps,
        int? ActualReps,
        decimal? ActualWeight,
        decimal? Rpe);
}
