using Mediator;
using Xenoh.Application.Common.Interfaces;
using Xenoh.Application.Common.Interfaces.Repositories;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;
using Xenoh.Domain.Entities;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Exercises.Commands.UpdateExercise;

public sealed class UpdateExerciseHandler(
    IExerciseRepository exerciseRepo,
    ICurrentUserService currentUser
) : IRequestHandler<UpdateExerciseCommand, ExerciseResponse>
{
    public async ValueTask<ExerciseResponse> Handle(UpdateExerciseCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var exercise = await exerciseRepo.FindWithSetsAndPlanAsync(request.ExerciseId, cancellationToken)
            ?? throw new InvalidOperationException("Exercise not found.");

        var plan = exercise.DailyWorkout.WeeklyWorkout.Plan;
        bool canEdit = plan.PlanType == PlanType.Coach
            ? plan.CreatedByCoachId == userId
            : plan.OwnerId == userId;

        if (!canEdit)
            throw new InvalidOperationException(
                plan.PlanType == PlanType.Coach && plan.OwnerId == userId
                    ? "This plan is managed by your coach and cannot be edited."
                    : "Access denied.");

        var plannedSetsChanged = request.PlannedSets is not null && request.PlannedSets.Value != exercise.PlannedSets;

        if (plannedSetsChanged)
        {
            bool hasCompletedSets = exercise.Sets.Any(s => s.IsCompleted);
            if (hasCompletedSets)
                throw new InvalidOperationException("Cannot change PlannedSets after sets have been completed.");

            exercise.PlannedSets = request.PlannedSets!.Value;

            int reps = request.PlannedReps ?? exercise.PlannedReps;
            decimal? weight = request.PlannedWeight ?? exercise.PlannedWeight;

            // Change only the delta. Recreating every set (remove-all + re-add) left the
            // removed entities marked Deleted *while still attached* to exercise.Sets, an
            // inconsistent tracker state that made EF emit a DELETE matching 0 rows
            // (DbUpdateConcurrencyException) when reducing the count.
            var ordered = exercise.Sets.OrderBy(s => s.SetNumber).ToList();

            if (ordered.Count > exercise.PlannedSets)
            {
                // Reduce: drop the surplus sets from both the DbSet and the navigation.
                var surplus = ordered.Skip(exercise.PlannedSets).ToList();
                exerciseRepo.RemoveSetRange(surplus);
                foreach (var set in surplus)
                    exercise.Sets.Remove(set);
            }
            else
            {
                // Grow: append the missing sets. Add them through the DbSet so EF marks
                // them Added — a brand-new set carries a non-default Guid from BaseEntity,
                // so adding it only to the tracked parent's navigation makes EF treat it
                // as an existing row (Modified) and emit an UPDATE that matches 0 rows.
                var newSets = new List<ExerciseSet>();
                for (int i = ordered.Count + 1; i <= exercise.PlannedSets; i++)
                {
                    var set = new ExerciseSet
                    {
                        SetNumber = i,
                        PlannedReps = reps,
                        PlannedWeight = weight,
                        ExerciseId = exercise.Id
                    };
                    exercise.Sets.Add(set);
                    newSets.Add(set);
                }
                exerciseRepo.AddSetRange(newSets);
            }

            // Apply the target reps/weight to the remaining sets and keep numbering contiguous.
            int setNumber = 1;
            foreach (var set in exercise.Sets.OrderBy(s => s.SetNumber))
            {
                set.SetNumber = setNumber++;
                set.PlannedReps = reps;
                set.PlannedWeight = weight;
            }
        }

        if (request.PlannedReps is not null)
        {
            exercise.PlannedReps = request.PlannedReps.Value;
            if (!plannedSetsChanged)
            {
                foreach (var set in exercise.Sets.Where(s => !s.IsCompleted))
                    set.PlannedReps = request.PlannedReps.Value;
            }
        }

        if (request.PlannedWeight is not null)
        {
            exercise.PlannedWeight = request.PlannedWeight;
            if (!plannedSetsChanged)
            {
                foreach (var set in exercise.Sets.Where(s => !s.IsCompleted))
                    set.PlannedWeight = request.PlannedWeight;
            }
        }

        if (request.Notes is not null) exercise.Notes = request.Notes;

        exercise.UpdatedAt = DateTime.UtcNow;

        await exerciseRepo.SaveChangesAsync(cancellationToken);

        return CreateExerciseHandler.ToResponse(exercise);
    }
}
