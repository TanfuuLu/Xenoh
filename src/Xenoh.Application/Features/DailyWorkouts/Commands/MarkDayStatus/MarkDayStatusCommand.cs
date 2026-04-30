using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.DailyWorkouts.Commands.MarkDayStatus;

public sealed record MarkDayStatusCommand(Guid DailyWorkoutId, DayStatus Status) : IRequest;

public sealed record MarkDayStatusRequest
{
    [Required]
    public required string Status { get; init; }
}
