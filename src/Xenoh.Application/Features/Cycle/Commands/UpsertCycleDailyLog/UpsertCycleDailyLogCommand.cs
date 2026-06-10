using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mediator;
using Xenoh.Domain.Enums;

namespace Xenoh.Application.Features.Cycle.Commands.UpsertCycleDailyLog;

public sealed record UpsertCycleDailyLogCommand : IRequest<CycleDailyLogResponse>
{
    [JsonIgnore]
    public DateOnly Date { get; init; }

    [EnumDataType(typeof(FlowIntensity), ErrorMessage = "Invalid flow value.")]
    public FlowIntensity? Flow { get; init; }

    public IReadOnlyList<CycleSymptoms>? Symptoms { get; init; }

    [EnumDataType(typeof(CycleMood), ErrorMessage = "Invalid mood value.")]
    public CycleMood? Mood { get; init; }

    [Range(1, 5, ErrorMessage = "Energy level must be between 1 and 5.")]
    public int? EnergyLevel { get; init; }

    [StringLength(500)]
    public string? Notes { get; init; }
}
