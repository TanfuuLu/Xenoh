using System.ComponentModel.DataAnnotations;
using Mediator;

namespace Xenoh.Application.Features.Cycle.Commands.UpdateCycleSettings;

public sealed record UpdateCycleSettingsCommand : IRequest<CycleSettingsResponse>
{
    [Range(15, 60, ErrorMessage = "Average cycle length must be between 15 and 60 days.")]
    public int? AverageCycleLengthOverride { get; init; }

    [Range(1, 10, ErrorMessage = "Average period length must be between 1 and 10 days.")]
    public int? AveragePeriodLengthOverride { get; init; }

    public bool ShareWithCoach { get; init; }
}
