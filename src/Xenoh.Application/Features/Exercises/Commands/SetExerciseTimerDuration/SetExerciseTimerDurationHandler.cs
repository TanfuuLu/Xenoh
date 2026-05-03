using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Commands.SetExerciseTimerDuration;

public sealed class SetExerciseTimerDurationHandler(
    IExerciseRepository exerciseRepo,
    IBodyweightRepository bodyweightRepo,
    IUserPrRepository userPrRepo,
    ICurrentUserService currentUser
) : IRequestHandler<SetExerciseTimerDurationCommand, ExerciseResponse>
{
    public async ValueTask<ExerciseResponse> Handle(
        SetExerciseTimerDurationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.DurationSeconds <= 0)
            throw new InvalidOperationException("Duration must be greater than zero.");

        var userId = currentUser.UserId;
        var exercise = await exerciseRepo.FindWithSetsAndPlanAsync(request.ExerciseId, cancellationToken)
            ?? throw new InvalidOperationException("Exercise not found.");

        var plan = exercise.DailyWorkout.WeeklyWorkout.Plan;
        if (plan.OwnerId != userId)
            throw new InvalidOperationException("Access denied.");

        exercise.DurationSeconds = request.DurationSeconds;
        exercise.UpdatedAt = DateTime.UtcNow;

        await exerciseRepo.SaveChangesAsync(cancellationToken);

        var prWeight = (await userPrRepo.GetByTemplateIdsAsync(
            userId, [exercise.ExerciseTemplateId], cancellationToken))
            .GetValueOrDefault(exercise.ExerciseTemplateId);
        var bodyweight = await bodyweightRepo.GetLatestWeightOnOrBeforeAsync(
            plan.OwnerId, exercise.DailyWorkout.Date, cancellationToken);

        return CreateExerciseHandler.ToResponse(exercise, prWeight, bodyweight);
    }
}
