using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;
using Xenoh.Application.Features.Exercises.Commands.MarkSetComplete;

namespace Xenoh.Application.Features.DailyWorkouts.Commands.CompleteDayWorkout;

public sealed class CompleteDayWorkoutHandler(
    IDailyWorkoutRepository dailyWorkoutRepo,
    IExerciseRepository exerciseRepo,
    IMediator mediator,
    ICurrentUserService currentUser
) : IRequestHandler<CompleteDayWorkoutCommand, List<ExerciseResponse>>
{
    public async ValueTask<List<ExerciseResponse>> Handle(
        CompleteDayWorkoutCommand request,
        CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var day = await dailyWorkoutRepo.FindWithExercisesAndPlanAsync(request.DailyWorkoutId, cancellationToken)
            ?? throw new InvalidOperationException("Daily workout not found.");

        var plan = day.WeeklyWorkout.Plan;
        if (plan.OwnerId != userId)
            throw new InvalidOperationException("Access denied.");

        var setsToComplete = day.Exercises
            .Where(e => !e.IsSkipped && !e.IsCompleted)
            .OrderBy(e => e.SortOrder)
            .ThenBy(e => e.CreatedAt)
            .SelectMany(e => e.Sets
                .Where(s => !s.IsCompleted)
                .OrderBy(s => s.SetNumber)
                .Select(s => new MarkSetCompleteCommand
                {
                    SetId = s.Id,
                    ActualReps = s.PlannedReps,
                    ActualWeight = s.PlannedWeight
                }))
            .ToList();

        foreach (var command in setsToComplete)
            await mediator.Send(command, cancellationToken);

        var result = await exerciseRepo.GetByDayWithPrsAsync(
            request.DailyWorkoutId,
            userId,
            1,
            int.MaxValue,
            cancellationToken);

        return result.Items.ToList();
    }
}
