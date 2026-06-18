using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Application.Features.Exercises.Commands.CreateExercise;

namespace Xenoh.Application.Features.Exercises.Commands.UpdateSetPlan;

public sealed record UpdateSetPlanCommand : IRequest<ExerciseResponse>
{
    public Guid SetId { get; init; }

    [Range(1, 1000)]
    public int? PlannedReps { get; init; }

    [Range(0, 10000)]
    public decimal? PlannedWeight { get; init; }
}
