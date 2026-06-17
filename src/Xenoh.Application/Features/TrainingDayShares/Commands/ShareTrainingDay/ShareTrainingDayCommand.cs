using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.TrainingDayShares.Commands.ShareTrainingDay;

public sealed record ShareTrainingDayCommand : IRequest<TrainingDayShareResponse>
{
    [Required]
    public required Guid DailyWorkoutId { get; init; }

    [MaxLength(500)]
    public string? Caption { get; init; }
}
