using System.ComponentModel.DataAnnotations;
using Mediator;
using Xenoh.Application.Features.Plans.Commands.CreatePlan;

namespace Xenoh.Application.Features.Plans.Commands.CreateAiStarterPlan;

public sealed record CreateAiStarterPlanCommand : IRequest<PlanResponse>
{
    [Required]
    [StringLength(30, MinimumLength = 3)]
    public required string Goal { get; init; }

    [Required]
    [StringLength(30, MinimumLength = 3)]
    public required string Experience { get; init; }

    [Range(2, 5)]
    public required int DaysPerWeek { get; init; }

    [Required]
    [StringLength(50, MinimumLength = 3)]
    public required string SplitPreference { get; init; }

    [Range(30, 90)]
    public required int SessionLengthMinutes { get; init; }

    [Required]
    [StringLength(200, MinimumLength = 2)]
    public required string Equipment { get; init; }

    [Required]
    public required DateOnly StartDate { get; init; }

    [Required]
    public required DateOnly EndDate { get; init; }

    [StringLength(100)]
    public string? Name { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }

    public string? Language { get; init; }
}
