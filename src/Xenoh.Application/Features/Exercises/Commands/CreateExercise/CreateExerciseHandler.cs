using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Exercises.Commands.CreateExercise;

public sealed class CreateExerciseHandler(
    IDailyWorkoutRepository dailyWorkoutRepo,
    IExerciseRepository exerciseRepo,
    IExerciseTemplateRepository exerciseTemplateRepo,
    IUserPrRepository userPrRepo,
    ICurrentUserService currentUser
) : IRequestHandler<CreateExerciseCommand, ExerciseResponse>
{
    public async ValueTask<ExerciseResponse> Handle(CreateExerciseCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var dailyWorkout = await dailyWorkoutRepo.FindWithPlanAsync(request.DailyWorkoutId, cancellationToken)
            ?? throw new InvalidOperationException("Daily workout not found.");

        var plan = dailyWorkout.WeeklyWorkout.Plan;
        bool canEdit = plan.PlanType == PlanType.Coach
            ? plan.CreatedByCoachId == userId
            : plan.OwnerId == userId;

        if (!canEdit)
            throw new InvalidOperationException(
                plan.PlanType == PlanType.Coach && plan.OwnerId == userId
                    ? "This plan is managed by your coach and cannot be edited."
                    : "Access denied.");

        var template = await exerciseTemplateRepo.FindByIdAsync(request.ExerciseTemplateId, cancellationToken)
            ?? throw new InvalidOperationException("Exercise template not found.");

        var exercise = new Exercise
        {
            ExerciseTemplateId = template.Id,
            Name = template.Name,
            PrimaryMuscleGroup = template.PrimaryMuscleGroup,
            SecondaryMuscleGroups = template.SecondaryMuscleGroups,
            PlannedSets = request.PlannedSets,
            PlannedReps = request.PlannedReps,
            PlannedWeight = request.PlannedWeight,
            Notes = request.Notes,
            DailyWorkoutId = request.DailyWorkoutId
        };

        for (int i = 1; i <= request.PlannedSets; i++)
        {
            exercise.Sets.Add(new ExerciseSet
            {
                SetNumber = i,
                PlannedReps = request.PlannedReps,
                PlannedWeight = request.PlannedWeight
            });
        }

        await exerciseRepo.AddAsync(exercise, cancellationToken);
        await exerciseRepo.SaveChangesAsync(cancellationToken);

        var prWeight = (await userPrRepo.GetByTemplateIdsAsync(
            userId, [exercise.ExerciseTemplateId], cancellationToken))
            .GetValueOrDefault(exercise.ExerciseTemplateId);

        return ToResponse(exercise, prWeight);
    }

    public static ExerciseResponse ToResponse(Exercise e, decimal? personalRecordWeight = null) => new(
        e.Id,
        e.ExerciseTemplateId,
        e.Name,
        e.PrimaryMuscleGroup.ToString(),
        e.SecondaryMuscleGroups.Select(m => m.ToString()).ToList(),
        e.PlannedSets,
        e.PlannedReps,
        e.PlannedWeight,
        e.Sets.Count(s => s.IsCompleted),
        e.IsCompleted,
        e.Notes,
        e.DailyWorkoutId,
        e.Sets.OrderBy(s => s.SetNumber).Select(s => new ExerciseSetResponse(
            s.Id,
            s.SetNumber,
            s.PlannedReps,
            s.PlannedWeight,
            s.ActualReps,
            s.ActualWeight,
            s.Rpe,
            s.IsCompleted,
            s.CompletedAt
        )).ToList(),
        personalRecordWeight
    );
}
