using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Commands.UpdateSetPlan;

/// <summary>
/// Edit a single set's planned reps/weight without completing it. Lets the athlete
/// adjust per-set targets during a workout (e.g. bump up the load on the last set).
/// </summary>
public sealed class UpdateSetPlanHandler(
    IExerciseSetRepository exerciseSetRepo,
    IUserPrRepository userPrRepo,
    IBodyweightRepository bodyweightRepo,
    ICurrentUserService currentUser
) : IRequestHandler<UpdateSetPlanCommand, ExerciseResponse>
{
    public async ValueTask<ExerciseResponse> Handle(UpdateSetPlanCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var set = await exerciseSetRepo.FindForCompleteAsync(request.SetId, cancellationToken)
            ?? throw new InvalidOperationException("Set not found.");

        var exercise = set.Exercise;
        var plan = exercise.DailyWorkout.WeeklyWorkout.Plan;

        if (plan.OwnerId != userId)
            throw new InvalidOperationException("Access denied.");

        if (set.IsCompleted)
            throw new InvalidOperationException("Cannot edit a completed set.");

        if (request.PlannedReps is not null) set.PlannedReps = request.PlannedReps.Value;
        if (request.PlannedWeight is not null) set.PlannedWeight = request.PlannedWeight;
        set.UpdatedAt = DateTime.UtcNow;

        await exerciseSetRepo.SaveChangesAsync(cancellationToken);

        var pr = await userPrRepo.FindAsync(userId, exercise.ExerciseTemplateId, cancellationToken);
        var bodyweight = await bodyweightRepo.GetLatestWeightOnOrBeforeAsync(
            plan.OwnerId,
            exercise.DailyWorkout.Date,
            cancellationToken);

        return CreateExerciseHandler.ToResponse(exercise, pr?.Weight, bodyweight);
    }
}
