using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Exercises.Commands.ReorderExercises;

public sealed class ReorderExercisesHandler(
    IExerciseRepository exerciseRepo,
    IUserPrRepository userPrRepo,
    ICurrentUserService currentUser
) : IRequestHandler<ReorderExercisesCommand, List<ExerciseResponse>>
{
    public async ValueTask<List<ExerciseResponse>> Handle(
        ReorderExercisesCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var requestedIds = request.ExerciseIds.Distinct().ToList();

        if (requestedIds.Count != request.ExerciseIds.Count)
            throw new InvalidOperationException("Exercise order contains duplicate IDs.");

        var exercises = await exerciseRepo.GetByIdsWithPlanAsync(requestedIds, cancellationToken);

        if (exercises.Count != requestedIds.Count)
            throw new InvalidOperationException("Exercise not found.");

        if (exercises.Any(e => e.DailyWorkoutId != request.DailyWorkoutId))
            throw new InvalidOperationException("All exercises must belong to the same daily workout.");

        var plan = exercises[0].DailyWorkout.WeeklyWorkout.Plan;
        var canEdit = plan.PlanType == PlanType.Coach
            ? plan.CreatedByCoachId == userId
            : plan.OwnerId == userId;

        if (!canEdit)
            throw new InvalidOperationException(
                plan.PlanType == PlanType.Coach && plan.OwnerId == userId
                    ? "This plan is managed by your coach and cannot be edited."
                    : "Access denied.");

        var byId = exercises.ToDictionary(e => e.Id);
        for (var i = 0; i < requestedIds.Count; i++)
        {
            var exercise = byId[requestedIds[i]];
            exercise.SortOrder = i;
            exercise.UpdatedAt = DateTime.UtcNow;
        }

        await exerciseRepo.SaveChangesAsync(cancellationToken);

        var templateIds = exercises.Select(e => e.ExerciseTemplateId).Distinct().ToList();
        var prs = await userPrRepo.GetByTemplateIdsAsync(userId, templateIds, cancellationToken);

        return requestedIds
            .Select(id =>
            {
                var exercise = byId[id];
                return CreateExerciseHandler.ToResponse(
                    exercise,
                    prs.GetValueOrDefault(exercise.ExerciseTemplateId));
            })
            .ToList();
    }
}
