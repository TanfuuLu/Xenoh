using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.DailyWorkouts.Commands.CopyDailyWorkout;

public sealed record CopyDailyWorkoutCommand(
    Guid SourceDailyWorkoutId,
    Guid TargetDailyWorkoutId
) : IRequest<CopyDailyWorkoutResponse>;

public sealed record CopyDailyWorkoutRequest
{
    [Required]
    public required Guid TargetDailyWorkoutId { get; init; }
}

public sealed record CopyDailyWorkoutResponse(
    Guid TargetDailyWorkoutId,
    int ExercisesCopied
);
