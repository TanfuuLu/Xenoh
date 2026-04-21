using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Application.Features.WeeklyWorkouts.Queries.GetWeeksByPlan;

namespace Xenoh.Application.Features.WeeklyWorkouts.Commands.UpdateWeeklyWorkout;

public sealed record UpdateWeeklyWorkoutCommand : IRequest<WeeklyWorkoutResponse>
{
    [Required]
    public required Guid WeeklyWorkoutId { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }
}
