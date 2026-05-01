using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Commands.FinishExerciseTimer;

public sealed class FinishExerciseTimerHandler(
    IExerciseRepository exerciseRepo,
    IBodyweightRepository bodyweightRepo,
    IUserPrRepository userPrRepo,
    ICurrentUserService currentUser
) : IRequestHandler<FinishExerciseTimerCommand, ExerciseResponse>
{
    public async ValueTask<ExerciseResponse> Handle(
        FinishExerciseTimerCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var exercise = await exerciseRepo.FindWithSetsAndPlanAsync(request.ExerciseId, cancellationToken)
            ?? throw new InvalidOperationException("Exercise not found.");

        var plan = exercise.DailyWorkout.WeeklyWorkout.Plan;
        if (plan.OwnerId != userId)
            throw new InvalidOperationException("Access denied.");

        if (exercise.StartedAtUtc is null)
            throw new InvalidOperationException("Exercise timer has not been started.");

        if (exercise.EndedAtUtc is null)
        {
            var endedAtUtc = DateTime.UtcNow;
            exercise.EndedAtUtc = endedAtUtc;
            exercise.DurationSeconds = Math.Max(0, (int)Math.Round((endedAtUtc - exercise.StartedAtUtc.Value).TotalSeconds));
            exercise.UpdatedAt = endedAtUtc;

            await exerciseRepo.SaveChangesAsync(cancellationToken);
        }

        var prWeight = (await userPrRepo.GetByTemplateIdsAsync(
            userId, [exercise.ExerciseTemplateId], cancellationToken))
            .GetValueOrDefault(exercise.ExerciseTemplateId);
        var bodyweight = await bodyweightRepo.GetLatestWeightOnOrBeforeAsync(
            plan.OwnerId, exercise.DailyWorkout.Date, cancellationToken);

        return CreateExerciseHandler.ToResponse(exercise, prWeight, bodyweight);
    }
}
