using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Commands.StartExerciseTimer;

public sealed class StartExerciseTimerHandler(
    IExerciseRepository exerciseRepo,
    IBodyweightRepository bodyweightRepo,
    IUserPrRepository userPrRepo,
    ICurrentUserService currentUser
) : IRequestHandler<StartExerciseTimerCommand, ExerciseResponse>
{
    public async ValueTask<ExerciseResponse> Handle(
        StartExerciseTimerCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var exercise = await exerciseRepo.FindWithSetsAndPlanAsync(request.ExerciseId, cancellationToken)
            ?? throw new InvalidOperationException("Exercise not found.");

        var plan = exercise.DailyWorkout.WeeklyWorkout.Plan;
        if (plan.OwnerId != userId)
            throw new InvalidOperationException("Access denied.");

        if (exercise.EndedAtUtc is not null)
            throw new InvalidOperationException("Exercise timer has already finished.");

        exercise.StartedAtUtc ??= DateTime.UtcNow;
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
